using System.Runtime.Versioning;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Estimates;

// Endpoint import dùng Microsoft.Office.Interop.Excel — chỉ chạy trên Windows
[SupportedOSPlatform("windows")]
public static class EstimateEndpoints
{
    public static IEndpointRouteBuilder MapEstimateEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/estimates").WithTags("Estimates")
            .RequireAuthorization();

        g.MapGet("/", ListAsync);
        g.MapGet("/{id:int}", GetAsync);
        g.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        g.MapPut("/{id:int}", UpdateAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        g.MapDelete("/{id:int}", DeleteAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));
        g.MapGet("/{id:int}/summary", GetCostSummaryAsync);
        g.MapGet("/{id:int}/items", GetItemsAsync);
        g.MapGet("/{estimateId:int}/items/{itemId:int}/details", GetItemDetailsAsync);
        g.MapPut("/{estimateId:int}/items/{itemId}/details/{detailId:int}", UpdateItemDetailAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        g.MapPut("/{estimateId:int}/items/{itemId}", UpdateItemAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));

        g.MapPost("/import", ImportFromExcelAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));

        return app;
    }

    // ====================================================================
    // LIST
    // ====================================================================

    private static async Task<IResult> ListAsync(
        [AsParameters] PagedRequest p,
        string? search,
        ServerBMCDbContext db,
        CancellationToken ct)
    {
        var query = db.Estimates.AsNoTracking()
            .Include(x => x.EstimateCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                x.EstimateCategory!.Name.Contains(s) ||
                x.DocumentType.Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(p.Skip).Take(p.Take)
            .Select(x => new
            {
                x.Id,
                CategoryName = x.EstimateCategory!.Name,
                x.DocumentType,
                x.DocumentNumber,
                x.TotalAmount,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<PagedResult<object>>.Ok(new PagedResult<object>
        {
            Items = items.Cast<object>().ToList(),
            Total = total,
            Page = p.Page ?? 1,
            PageSize = p.PageSize ?? 20
        }));
    }

    // ====================================================================
    // GET
    // ====================================================================

    private static async Task<IResult> GetAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var estimate = await db.Estimates.AsNoTracking()
            .Include(x => x.Items).ThenInclude(w => w.Details)
            .Include(x => x.CostSummary)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (estimate == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy dự toán"));

        var dto = new EstimateService(db).MapToDto(estimate);
        return Results.Ok(ApiResponse<object>.Ok(dto));
    }

    // ====================================================================
    // CREATE
    // ====================================================================

    private static async Task<IResult> CreateAsync(
        CreateEstimateDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var service = new EstimateService(db);
        var estimate = await service.CreateAsync(dto, userId, ct);

        await audit.WriteAsync(db, userId, "Create", "Estimate", estimate.Id, null, dto,
            "Tạo dự toán", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { estimate.Id }, "Tạo dự toán thành công"));
    }

    // ====================================================================
    // UPDATE
    // ====================================================================

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateEstimateDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var estimate = await db.Estimates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (estimate == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy dự toán"));

        var old = new { estimate.DocumentType, estimate.DocumentNumber };

        estimate.DocumentType = dto.DocumentType;
        estimate.DocumentNumber = dto.DocumentNumber;
        estimate.DocumentDate = dto.DocumentDate;
        estimate.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "Estimate", id, old, dto,
            "Cập nhật dự toán", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Cập nhật thành công"));
    }

    // ====================================================================
    // DELETE
    // ====================================================================

    private static async Task<IResult> DeleteAsync(
        int id,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var estimate = await db.Estimates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (estimate == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy dự toán"));

        db.Estimates.Remove(estimate);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "Estimate", id, estimate, null,
            "Xóa dự toán", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Đã xóa dự toán"));
    }

    // ====================================================================
    // CHILD COLLECTIONS
    // ====================================================================

    private static async Task<IResult> GetCostSummaryAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var summary = await db.CostSummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EstimateId == id, ct);

        if (summary == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy bảng tổng hợp kinh phí"));

        return Results.Ok(ApiResponse<object>.Ok(summary));
    }

    private static async Task<IResult> GetItemsAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.EstimateItems.AsNoTracking()
            .Where(x => x.EstimateId == id)
            .OrderBy(x => x.Stt)
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> GetItemDetailsAsync(
        int estimateId,
        int itemId,
        ServerBMCDbContext db,
        CancellationToken ct)
    {
        var details = await db.EstimateItemDetails.AsNoTracking()
            .Where(x => x.EstimateItemId == itemId)
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(details));
    }

    // ====================================================================
    // UPDATE DETAIL
    // ====================================================================

    private static async Task<IResult> UpdateItemDetailAsync(
        int estimateId,
        int itemId,
        int detailId,
        UpdateEstimateItemDetailDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var detail = await db.EstimateItemDetails.FirstOrDefaultAsync(
            x => x.Id == detailId && x.EstimateItemId == itemId, ct);

        if (detail == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy chi tiết"));

        detail.Quantity = dto.Quantity;
        detail.UnitPrice = dto.UnitPrice;
        detail.Factor = dto.Factor;
        detail.TotalAmount = dto.Quantity * dto.UnitPrice * dto.Factor;

        var item = await db.EstimateItems.FindAsync(new object[] { itemId }, ct);
        if (item != null)
        {
            var service = new EstimateService(db);
            service.RecalculateItemTotals(item);
            await db.SaveChangesAsync(ct);

            // Tính lại toàn bộ Estimate — EstimateService lo phần TotalAmount
            service.RecalculateEstimateTotals(estimateId);
            await db.SaveChangesAsync(ct);
        }

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "EstimateItemDetail", detailId,
            null, dto, "Cập nhật chi tiết định mức", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);

        return Results.Ok(ApiResponse<object>.Ok(new { detailId }, "Cập nhật thành công"));
    }

    // ====================================================================
    // UPDATE ITEM
    // ====================================================================

    private static async Task<IResult> UpdateItemAsync(
        int estimateId,
        int itemId,
        UpdateEstimateItemDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var item = await db.EstimateItems.FirstOrDefaultAsync(
            x => x.Id == itemId && x.EstimateId == estimateId, ct);

        if (item == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy hạng mục"));

        item.Quantity = dto.Quantity;

        var service = new EstimateService(db);
        service.RecalculateItemTotals(item);

        await db.SaveChangesAsync(ct);
        service.RecalculateEstimateTotals(estimateId);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "EstimateItem", itemId,
            null, dto, "Cập nhật hạng mục", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);

        return Results.Ok(ApiResponse<object>.Ok(new { itemId }, "Cập nhật thành công"));
    }

    // ====================================================================
    // IMPORT EXCEL
    // ====================================================================

    private static async Task<IResult> ImportFromExcelAsync(
        [FromBody] ImportExcelDto dto,
        ServerBMCDbContext db,
        IWebHostEnvironment env,
        ClaimsPrincipal principal,
        IAuditWriter audit,
        HttpContext http,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.FilePath))
            return Results.BadRequest(ApiResponse<object>.Fail("Đường dẫn file không hợp lệ"));

        var filePath = dto.FilePath;
        if (!Path.IsPathRooted(filePath))
            filePath = Path.Combine(env.ContentRootPath, filePath);

        if (!File.Exists(filePath))
            return Results.NotFound(ApiResponse<object>.Fail($"File không tồn tại: {filePath}"));

        if (!dto.EstimateCategoryId.HasValue)
            return Results.BadRequest(ApiResponse<object>.Fail("EstimateCategoryId là bắt buộc"));

        try
        {
            var service = new EstimateService(db);
            var estimateId = await ImportExcelWindowsAsync(service, filePath, dto.EstimateCategoryId.Value, ct);

            var estimate = await db.Estimates.FindAsync(new object[] { estimateId }, ct);
            await audit.WriteAsync(db, principal.GetUserId(), "Import", "Estimate", estimateId, null, new { filePath },
                $"Import dự toán từ Excel: {filePath}",
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(), ct);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                estimateId,
                categoryName = estimate?.EstimateCategory?.Name,
                totalAmount = estimate?.TotalAmount,
                message = "Import thành công từ Excel"
            }, "Import dữ liệu từ Excel thành công"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Lỗi import: {ex.Message}"));
        }
    }

    // Bọc lại để tránh CA1416 — endpoint import chỉ nên dùng trên Windows server
    private static Task<int> ImportExcelWindowsAsync(EstimateService service, string filePath, int estimateCategoryId, CancellationToken ct)
        => service.ImportFromExcelAsync(filePath, estimateCategoryId, ct);
}
