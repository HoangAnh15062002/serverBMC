using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Projects;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/projects").WithTags("Projects")
            .RequireAuthorization();

        g.MapGet("/", ListAsync);
        g.MapGet("/{id:int}", GetAsync);
        g.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        g.MapPut("/{id:int}", UpdateAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        g.MapDelete("/{id:int}", DeleteAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        var l = app.MapGroup("/api/projects/{projectId:int}/lots").WithTags("ProjectLots")
            .RequireAuthorization();

        l.MapGet("/", ListLotsAsync);
        l.MapGet("/{lotId:int}", GetLotAsync);
        l.MapPost("/", CreateLotAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        l.MapPut("/{lotId:int}", UpdateLotAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        l.MapDelete("/{lotId:int}", DeleteLotAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        return app;
    }

    // ============ Projects ============

    private static async Task<IResult> ListAsync(
        [AsParameters] PagedRequest p,
        string? status,
        ServerBMCDbContext db, CancellationToken ct)
    {
        var q = db.ProjectLots.Where(l => l.ProjectId == 0).Select(l => l.ProjectId); // dummy to keep compiler happy
        var query = db.Projects.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.Trim();
            query = query.Where(x => x.ProjectCode.Contains(s) || x.ProjectName.Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(p.Skip).Take(p.Take)
            .Select(x => new
            {
                x.Id, x.ProjectCode, x.ProjectName, x.ProjectType, x.Location,
                x.Investor, x.Contractor, x.ContractValue, x.StartDate, x.EndDate,
                x.Status, x.CreatedAt
            })
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>
        {
            Items = items.Cast<object>().ToList(),
            Total = total, Page = p.Page, PageSize = p.PageSize
        }));
    }

    private static async Task<IResult> GetAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var p = await db.Projects.AsNoTracking()
            .Include(x => x.Lots)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? Results.NotFound() : Results.Ok(ApiResponse<object>.Ok(p));
    }

    private static async Task<IResult> CreateAsync(
        ProjectCreateDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        if (await db.Projects.AnyAsync(x => x.ProjectCode == dto.ProjectCode, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Mã dự án đã tồn tại"));

        var userId = principal.GetUserId();
        var entity = new Project
        {
            ProjectCode = dto.ProjectCode,
            ProjectName = dto.ProjectName,
            ProjectType = dto.ProjectType,
            Location = dto.Location,
            Investor = dto.Investor,
            Contractor = dto.Contractor,
            ContractValue = dto.ContractValue,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status ?? "Đang thi công",
            Description = dto.Description,
            CreatedBy = userId
        };
        db.Projects.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "Project", entity.Id, null, entity,
            "Tạo dự án", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }, "Tạo dự án thành công"));
    }

    private static async Task<IResult> UpdateAsync(
        int id, ProjectUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var e = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Results.NotFound();

        var old = new { e.ProjectName, e.ProjectType, e.Location, e.Investor, e.Contractor, e.ContractValue, e.StartDate, e.EndDate, e.Status };
        e.ProjectName = dto.ProjectName;
        e.ProjectType = dto.ProjectType;
        e.Location = dto.Location;
        e.Investor = dto.Investor;
        e.Contractor = dto.Contractor;
        e.ContractValue = dto.ContractValue;
        e.StartDate = dto.StartDate;
        e.EndDate = dto.EndDate;
        if (dto.Status is not null) e.Status = dto.Status;
        e.Description = dto.Description;
        e.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "Project", id, old, dto,
            "Cập nhật dự án", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Cập nhật thành công"));
    }

    private static async Task<IResult> DeleteAsync(
        int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var e = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Results.NotFound();

        db.Projects.Remove(e);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "Project", id, e, null,
            "Xóa dự án", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Đã xóa dự án"));
    }

    // ============ Lots ============

    private static async Task<IResult> ListLotsAsync(int projectId, ServerBMCDbContext db, CancellationToken ct)
    {
        var lots = await db.ProjectLots.AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.LotCode)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(lots));
    }

    private static async Task<IResult> GetLotAsync(int projectId, int lotId, ServerBMCDbContext db, CancellationToken ct)
    {
        var lot = await db.ProjectLots.AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProjectId == projectId && l.Id == lotId, ct);
        return lot is null ? Results.NotFound() : Results.Ok(ApiResponse<object>.Ok(lot));
    }

    private static async Task<IResult> CreateLotAsync(
        int projectId, ProjectLotCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
            return Results.NotFound(ApiResponse<object>.Fail("Dự án không tồn tại"));
        if (await db.ProjectLots.AnyAsync(l => l.ProjectId == projectId && l.LotCode == dto.LotCode, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Mã lô đã tồn tại trong dự án này"));

        var userId = principal.GetUserId();
        var lot = new ProjectLot
        {
            ProjectId = projectId,
            LotCode = dto.LotCode,
            LotName = dto.LotName,
            LotType = dto.LotType,
            Area = dto.Area,
            FloorCount = dto.FloorCount,
            UnitCount = dto.UnitCount,
            Status = dto.Status ?? "Chưa triển khai",
            Description = dto.Description,
            CreatedBy = userId
        };
        db.ProjectLots.Add(lot);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(db, userId, "Create", "ProjectLot", lot.Id, null, lot,
            $"Tạo lô {lot.LotCode}", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { lot.Id }, "Tạo lô thành công"));
    }

    private static async Task<IResult> UpdateLotAsync(
        int projectId, int lotId, ProjectLotUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var lot = await db.ProjectLots.FirstOrDefaultAsync(l => l.ProjectId == projectId && l.Id == lotId, ct);
        if (lot is null) return Results.NotFound();

        var old = new { lot.LotName, lot.LotType, lot.Area, lot.FloorCount, lot.UnitCount, lot.Status };
        lot.LotName = dto.LotName;
        lot.LotType = dto.LotType;
        lot.Area = dto.Area;
        lot.FloorCount = dto.FloorCount;
        lot.UnitCount = dto.UnitCount;
        if (dto.Status is not null) lot.Status = dto.Status;
        lot.Description = dto.Description;
        lot.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "ProjectLot", lotId, old, dto,
            $"Cập nhật lô {lot.LotCode}", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { lotId }));
    }

    private static async Task<IResult> DeleteLotAsync(
        int projectId, int lotId,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var lot = await db.ProjectLots.FirstOrDefaultAsync(l => l.ProjectId == projectId && l.Id == lotId, ct);
        if (lot is null) return Results.NotFound();
        db.ProjectLots.Remove(lot);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "ProjectLot", lotId, lot, null,
            $"Xóa lô {lot.LotCode}", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { lotId }, "Đã xóa lô"));
    }
}