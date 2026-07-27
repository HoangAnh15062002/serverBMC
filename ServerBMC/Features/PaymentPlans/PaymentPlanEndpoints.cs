using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.PaymentPlans;

public static class PaymentPlanEndpoints
{
    public static IEndpointRouteBuilder MapPaymentPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/payment-plans").WithTags("PaymentPlans")
            .RequireAuthorization();

        g.MapGet("/", ListAsync);
        g.MapGet("/{id:int}", GetAsync);
        g.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "Director"));
        g.MapPut("/{id:int}", UpdateAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "Director"));
        g.MapDelete("/{id:int}", DeleteAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        g.MapPost("/{id:int}/approve", ApproveAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        return app;
    }

    private static async Task<IResult> ListAsync(int? projectId, string? status, ServerBMCDbContext db, CancellationToken ct)
    {
        var q = db.PaymentPlans.AsNoTracking().AsQueryable();
        if (projectId.HasValue) q = q.Where(p => p.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.PaymentStatus == status);
        var items = await q.OrderByDescending(p => p.PlannedDate).ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> GetAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var p = await db.PaymentPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? Results.NotFound() : Results.Ok(ApiResponse<object>.Ok(p));
    }

    private static async Task<IResult> CreateAsync(
        PaymentPlanCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == dto.ProjectId, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Dự án không tồn tại"));

        var userId = principal.GetUserId();
        var entity = new PaymentPlan
        {
            ProjectId = dto.ProjectId,
            PlanTitle = dto.PlanTitle,
            PlanAmount = dto.PlanAmount,
            PlannedDate = dto.PlannedDate,
            PaymentStatus = dto.PaymentStatus ?? "Chưa giải ngân",
            ContractNumber = dto.ContractNumber,
            InvoiceNumber = dto.InvoiceNumber,
            Notes = dto.Notes,
            CreatedBy = userId
        };
        db.PaymentPlans.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "PaymentPlan", entity.Id, null, entity,
            "Tạo kế hoạch giải ngân", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }));
    }

    private static async Task<IResult> UpdateAsync(
        int id, PaymentPlanUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var p = await db.PaymentPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();
        var old = new { p.PlanTitle, p.PlanAmount, p.PlannedDate, p.ActualAmount, p.PaymentStatus };
        p.PlanTitle = dto.PlanTitle;
        p.PlanAmount = dto.PlanAmount;
        p.PlannedDate = dto.PlannedDate;
        p.ActualAmount = dto.ActualAmount;
        p.ActualDate = dto.ActualDate;
        if (dto.PaymentStatus is not null) p.PaymentStatus = dto.PaymentStatus;
        p.ContractNumber = dto.ContractNumber;
        p.InvoiceNumber = dto.InvoiceNumber;
        p.Notes = dto.Notes;
        p.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "PaymentPlan", id, old, dto,
            "Cập nhật kế hoạch giải ngân", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }

    private static async Task<IResult> DeleteAsync(
        int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var p = await db.PaymentPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();
        db.PaymentPlans.Remove(p);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "PaymentPlan", id, p, null,
            "Xóa kế hoạch giải ngân", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Đã xóa"));
    }

    private static async Task<IResult> ApproveAsync(
        int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var p = await db.PaymentPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();
        p.PaymentStatus = "Đã duyệt";
        p.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Approve", "PaymentPlan", id, null, null,
            "Duyệt kế hoạch giải ngân", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Đã duyệt"));
    }
}