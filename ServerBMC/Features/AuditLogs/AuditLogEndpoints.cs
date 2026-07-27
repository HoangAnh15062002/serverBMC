using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.AuditLogs;

public static class AuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/audit-logs").WithTags("AuditLogs")
            .RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        g.MapGet("/", ListAsync);
        return app;
    }

    private static async Task<IResult> ListAsync(
        int? userId, string? entityType, string? action,
        DateTime? from, DateTime? to,
        int page, int pageSize,
        ServerBMCDbContext db, CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;

        var q = db.AuditLogs.AsNoTracking().AsQueryable();
        if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(x => x.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action == action);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Results.Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>
        {
            Items = items.Cast<object>().ToList(),
            Total = total, Page = page, PageSize = pageSize
        }));
    }
}