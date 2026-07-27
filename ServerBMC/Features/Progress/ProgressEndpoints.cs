using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Progress;

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/categories/{categoryId:int}/progress").WithTags("Progress")
            .RequireAuthorization();

        g.MapGet("/", ListAsync);
        g.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireRole("Admin", "Engineer", "Director"));

        // Module 4 dashboard
        var w = app.MapGroup("/api/warnings").WithTags("Warnings").RequireAuthorization();
        w.MapGet("/", ListWarningsAsync);
        w.MapGet("/schedule", ScheduleWarningsAsync);
        w.MapPost("/{id:int}/resolve", ResolveAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director", "VP"));

        return app;
    }

    private static async Task<IResult> ListAsync(int categoryId, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.Progresses.AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.ProgressDate)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> CreateAsync(
        int categoryId, ProgressCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (cat is null) return Results.NotFound(ApiResponse<object>.Fail("Hạng mục không tồn tại"));

        var userId = principal.GetUserId();
        var variance = dto.PlannedPercent.HasValue ? dto.ProgressPercent - dto.PlannedPercent.Value : (decimal?)null;

        var p = new Domain.Entities.Progress
        {
            CategoryId = categoryId,
            ProgressDate = dto.ProgressDate,
            ProgressPercent = dto.ProgressPercent,
            PlannedPercent = dto.PlannedPercent,
            Variance = variance,
            Notes = dto.Notes,
            CreatedBy = userId
        };
        db.Progresses.Add(p);

        // Đồng bộ % hoàn thành lên Category
        cat.ProgressPercent = dto.ProgressPercent;
        cat.UpdatedAt = DateTime.UtcNow;
        if (cat.ActualStartDate is null) cat.ActualStartDate = dto.ProgressDate;
        if (dto.ProgressPercent >= 100m) cat.ActualEndDate = dto.ProgressDate;

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "Progress", p.Id, null, p,
            "Cập nhật tiến độ", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);

        // Phát cảnh báo chậm tiến độ
        if (dto.PlannedPercent.HasValue && dto.PlannedPercent.Value > 0)
        {
            var ratio = dto.ProgressPercent / dto.PlannedPercent.Value;
            int? level = ratio < 0.5m ? 4 : ratio < 0.8m ? 3 : (int?)null;
            if (level.HasValue && cat.PlannedEndDate < DateTime.UtcNow && dto.ProgressPercent < 100m)
                level = 4;
            if (level.HasValue)
            {
                db.Warnings.Add(new Warning
                {
                    WarningType = "ScheduleDelay",
                    WarningLevel = level.Value,
                    ProjectId = await GetProjectIdAsync(db, categoryId, ct),
                    Title = "Chậm tiến độ",
                    Message = $"Hạng mục '{cat.CategoryName}': KH={dto.PlannedPercent:0.##}% / TT={dto.ProgressPercent:0.##}%"
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { p.Id }));
    }

    private static async Task<int?> GetProjectIdAsync(ServerBMCDbContext db, int categoryId, CancellationToken ct)
        => await db.Categories.Where(c => c.Id == categoryId)
            .Select(c => (int?)c.ProjectLot.ProjectId).FirstOrDefaultAsync(ct);

    private static async Task<IResult> ListWarningsAsync(
        bool? unresolved, int? projectId, ServerBMCDbContext db, CancellationToken ct)
    {
        var q = db.Warnings.AsNoTracking().AsQueryable();
        if (unresolved == true) q = q.Where(w => !w.IsResolved);
        if (projectId.HasValue) q = q.Where(w => w.ProjectId == projectId.Value);
        var items = await q.OrderByDescending(w => w.CreatedAt).Take(200).ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> ScheduleWarningsAsync(int? projectId, ServerBMCDbContext db, CancellationToken ct)
    {
        var q = from c in db.Categories.AsNoTracking()
                join l in db.ProjectLots.AsNoTracking() on c.ProjectLotId equals l.Id
                join p in db.Projects.AsNoTracking() on l.ProjectId equals p.Id
                select new { c, l, p };

        if (projectId.HasValue) q = q.Where(x => x.p.Id == projectId.Value);

        var rows = await q.ToListAsync(ct);
        var now = DateTime.UtcNow;
        var result = rows.Select(x =>
        {
            decimal planned = x.c.PlannedEndDate.HasValue && x.c.PlannedStartDate.HasValue
                ? Math.Clamp((decimal)((now - x.c.PlannedStartDate.Value).TotalDays
                    / Math.Max(1, (x.c.PlannedEndDate.Value - x.c.PlannedStartDate.Value).TotalDays)) * 100m, 0m, 100m)
                : x.c.ProgressPercent;
            int overdue = x.c.PlannedEndDate.HasValue && x.c.PlannedEndDate.Value < now && x.c.ProgressPercent < 100m
                ? (int)(now - x.c.PlannedEndDate.Value).TotalDays : 0;
            string level;
            if (overdue > 0) level = "Overdue";
            else if (x.c.ProgressPercent < planned * 0.5m) level = "Late";
            else if (x.c.ProgressPercent < planned * 0.8m) level = "Early";
            else level = "None";
            return new ProgressWarningDto(x.c.Id, x.c.CategoryName, x.l.LotName, x.p.ProjectName,
                x.c.PlannedEndDate, x.c.ProgressPercent, planned, overdue, level);
        }).Where(r => r.Level != "None").ToList();

        return Results.Ok(ApiResponse<object>.Ok(result));
    }

    private static async Task<IResult> ResolveAsync(
        int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var w = await db.Warnings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return Results.NotFound();
        w.IsResolved = true;
        w.IsRead = true;
        w.ResolvedAt = DateTime.UtcNow;
        w.ResolvedBy = principal.GetUserId();

        await audit.WriteAsync(db, principal.GetUserId(), "Resolve", "Warning", id, null, null,
            "Đánh dấu cảnh báo đã xử lý", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }
}