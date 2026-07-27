using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.WorkItems;

public static class WorkItemEndpoints
{
    public static IEndpointRouteBuilder MapWorkItemEndpoints(this IEndpointRouteBuilder app)
    {
        // WorkItems (nested under SubCategory)
        var w = app.MapGroup("/api/subcategories/{subId:int}/workitems").WithTags("WorkItems")
            .RequireAuthorization();
        w.MapGet("/", ListWorkItemsAsync);
        w.MapGet("/{id:int}", GetWorkItemAsync);
        w.MapPost("/", CreateWorkItemAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director", "Engineer"));
        w.MapPut("/{id:int}", UpdateWorkItemAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director", "Engineer"));
        w.MapDelete("/{id:int}", DeleteWorkItemAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        // UnitPrices
        var up = app.MapGroup("/api/workitems/{id:int}/prices").WithTags("UnitPrices")
            .RequireAuthorization();
        up.MapGet("/", ListPricesAsync);
        up.MapPost("/", AddPriceAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        up.MapDelete("/{priceId:int}", DeletePriceAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        // ActualCosts
        var ac = app.MapGroup("/api/workitems/{id:int}/costs").WithTags("ActualCosts")
            .RequireAuthorization();
        ac.MapGet("/", ListCostsAsync);
        ac.MapPost("/", AddCostAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Accountant", "Director"));
        ac.MapDelete("/{costId:int}", DeleteCostAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "Director"));

        // AcceptedQuantities
        var aq = app.MapGroup("/api/workitems/{id:int}/accepted").WithTags("AcceptedQuantities")
            .RequireAuthorization();
        aq.MapGet("/", ListAcceptedAsync);
        aq.MapPost("/", AddAcceptedAsync).RequireAuthorization(p => p.RequireRole("Admin", "Engineer", "Director"));
        aq.MapDelete("/{accId:int}", DeleteAcceptedAsync).RequireAuthorization(p => p.RequireRole("Admin", "Engineer", "Director"));

        // Module 3 reports
        var reports = app.MapGroup("/api/reports/cost").WithTags("CostReports").RequireAuthorization();
        reports.MapGet("/profit-by-workitem/{workItemId:int}", ProfitByWorkItemAsync);
        reports.MapGet("/profit-by-category/{categoryId:int}", ProfitByCategoryAsync);
        reports.MapGet("/profit-by-lot/{lotId:int}", ProfitByLotAsync);
        reports.MapGet("/profit-by-project/{projectId:int}", ProfitByProjectAsync);
        reports.MapGet("/category-cost-compare/{categoryId:int}", CategoryCostCompareAsync);

        return app;
    }

    // ============ WorkItems ============

    private static async Task<IResult> ListWorkItemsAsync(int subId, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.WorkItems.AsNoTracking()
            .Where(w => w.SubCategoryId == subId && w.IsActive)
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Id)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> GetWorkItemAsync(int subId, int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var w = await db.WorkItems.AsNoTracking()
            .Include(x => x.UnitPrices)
            .FirstOrDefaultAsync(x => x.SubCategoryId == subId && x.Id == id, ct);
        return w is null ? Results.NotFound() : Results.Ok(ApiResponse<object>.Ok(w));
    }

    private static async Task<IResult> CreateWorkItemAsync(
        int subId, WorkItemCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.SubCategories.AnyAsync(s => s.Id == subId, ct))
            return Results.NotFound(ApiResponse<object>.Fail("Hạng mục phụ không tồn tại"));
        if (await db.WorkItems.AnyAsync(w => w.SubCategoryId == subId && w.ItemCode == dto.ItemCode, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Mã đầu mục đã tồn tại"));

        var userId = principal.GetUserId();
        var entity = new Domain.Entities.WorkItem
        {
            SubCategoryId = subId,
            ItemCode = dto.ItemCode,
            ItemName = dto.ItemName,
            Unit = dto.Unit,
            StandardQuantity = dto.StandardQuantity,
            MaterialNorm = dto.MaterialNorm,
            LaborNorm = dto.LaborNorm,
            MachineNorm = dto.MachineNorm,
            SortOrder = dto.SortOrder,
            Description = dto.Description,
            CreatedBy = userId
        };
        db.WorkItems.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "WorkItem", entity.Id, null, entity,
            "Tạo đầu mục công tác", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }));
    }

    private static async Task<IResult> UpdateWorkItemAsync(
        int subId, int id, WorkItemUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var w = await db.WorkItems.FirstOrDefaultAsync(x => x.SubCategoryId == subId && x.Id == id, ct);
        if (w is null) return Results.NotFound();
        var old = new { w.ItemName, w.Unit, w.StandardQuantity };
        w.ItemName = dto.ItemName;
        w.Unit = dto.Unit;
        w.StandardQuantity = dto.StandardQuantity;
        w.MaterialNorm = dto.MaterialNorm;
        w.LaborNorm = dto.LaborNorm;
        w.MachineNorm = dto.MachineNorm;
        w.SortOrder = dto.SortOrder;
        if (dto.IsActive.HasValue) w.IsActive = dto.IsActive.Value;
        w.Description = dto.Description;
        w.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "WorkItem", id, old, dto,
            "Cập nhật đầu mục", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }

    private static async Task<IResult> DeleteWorkItemAsync(
        int subId, int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var w = await db.WorkItems.FirstOrDefaultAsync(x => x.SubCategoryId == subId && x.Id == id, ct);
        if (w is null) return Results.NotFound();
        db.WorkItems.Remove(w);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "WorkItem", id, w, null,
            "Xóa đầu mục", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Đã xóa"));
    }

    // ============ UnitPrices ============

    private static async Task<IResult> ListPricesAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var prices = await db.UnitPrices.AsNoTracking()
            .Where(p => p.WorkItemId == id)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(prices));
    }

    private static async Task<IResult> AddPriceAsync(
        int id, UnitPriceCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.WorkItems.AnyAsync(w => w.Id == id, ct))
            return Results.NotFound(ApiResponse<object>.Fail("Đầu mục không tồn tại"));
        if (await db.UnitPrices.AnyAsync(p => p.WorkItemId == id && p.PriceType == dto.PriceType && p.EffectiveFrom == dto.EffectiveFrom, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Đơn giá cho loại/ngày này đã tồn tại"));

        var userId = principal.GetUserId();
        var p = new UnitPrice
        {
            WorkItemId = id, PriceType = dto.PriceType, UnitPriceValue = dto.UnitPriceValue,
            EffectiveFrom = dto.EffectiveFrom, EffectiveTo = dto.EffectiveTo, Notes = dto.Notes,
            CreatedBy = userId
        };
        db.UnitPrices.Add(p);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "UnitPrice", p.Id, null, p,
            "Thêm đơn giá", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { p.Id }));
    }

    private static async Task<IResult> DeletePriceAsync(
        int id, int priceId, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var p = await db.UnitPrices.FirstOrDefaultAsync(x => x.WorkItemId == id && x.Id == priceId, ct);
        if (p is null) return Results.NotFound();
        db.UnitPrices.Remove(p);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "UnitPrice", priceId, p, null,
            "Xóa đơn giá", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { priceId }, "Đã xóa"));
    }

    // ============ ActualCosts ============

    private static async Task<IResult> ListCostsAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var costs = await db.ActualCosts.AsNoTracking()
            .Where(c => c.WorkItemId == id)
            .OrderByDescending(c => c.CostDate)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(costs));
    }

    private static async Task<IResult> AddCostAsync(
        int id, ActualCostCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var w = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return Results.NotFound(ApiResponse<object>.Fail("Đầu mục không tồn tại"));

        var userId = principal.GetUserId();
        var c = new ActualCost
        {
            WorkItemId = id,
            CostType = dto.CostType,
            CostDate = dto.CostDate,
            Quantity = dto.Quantity,
            UnitPriceValue = dto.UnitPriceValue,
            TotalAmount = dto.TotalAmount,
            InvoiceNumber = dto.InvoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            Supplier = dto.Supplier,
            Description = dto.Description,
            CreatedBy = userId
        };
        db.ActualCosts.Add(c);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(db, userId, "Create", "ActualCost", c.Id, null, c,
            "Thêm chi phí thực tế", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);

        // Real-time cost warning (Module 3.5)
        var warning = await EvaluateCostWarningAsync(db, id, w, ct);
        if (warning is not null)
        {
            db.Warnings.Add(warning);
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { c.Id, warning = warning?.WarningLevel }));
    }

    private static async Task<Warning?> EvaluateCostWarningAsync(ServerBMCDbContext db, int workItemId, WorkItem w, CancellationToken ct)
    {
        var accepted = await db.AcceptedQuantities.Where(a => a.WorkItemId == workItemId)
            .SumAsync(a => (decimal?)a.AcceptedQuantityValue, ct) ?? 0m;
        var bidPrice = await db.UnitPrices.Where(u => u.WorkItemId == workItemId && u.PriceType == "VL")
            .OrderByDescending(u => u.EffectiveFrom)
            .Select(u => (decimal?)u.UnitPriceValue).FirstOrDefaultAsync(ct) ?? 0m;
        var actualTotal = await db.ActualCosts.Where(c => c.WorkItemId == workItemId)
            .SumAsync(c => (decimal?)c.TotalAmount, ct) ?? 0m;

        var bidRevenue = accepted * bidPrice;
        if (bidRevenue <= 0) return null;

        var pct = actualTotal / bidRevenue * 100m;
        int level; string title; string msg;
        if (pct >= 100m) { level = 4; title = "Vượt chi phí"; msg = $"Đầu mục '{w.ItemName}' đã lỗ: CP TT = {pct:0.##}% DT dự thầu"; }
        else if (pct >= 90m) { level = 2; title = "Cảnh báo chi phí"; msg = $"Đầu mục '{w.ItemName}' sắp lỗ: CP TT = {pct:0.##}% DT dự thầu"; }
        else return null;

        return new Warning
        {
            WarningType = "CostOverrun",
            WarningLevel = level,
            ProjectId = await GetProjectIdByWorkItemAsync(db, workItemId, ct),
            Title = title,
            Message = msg
        };
    }

    private static async Task<int?> GetProjectIdByWorkItemAsync(ServerBMCDbContext db, int workItemId, CancellationToken ct)
        => await db.WorkItems.Where(w => w.Id == workItemId)
            .Select(w => (int?)w.SubCategory.Category.ProjectLot.ProjectId).FirstOrDefaultAsync(ct);

    private static async Task<IResult> DeleteCostAsync(
        int id, int costId, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var c = await db.ActualCosts.FirstOrDefaultAsync(x => x.WorkItemId == id && x.Id == costId, ct);
        if (c is null) return Results.NotFound();
        db.ActualCosts.Remove(c);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "ActualCost", costId, c, null,
            "Xóa chi phí thực tế", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { costId }, "Đã xóa"));
    }

    // ============ AcceptedQuantities ============

    private static async Task<IResult> ListAcceptedAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.AcceptedQuantities.AsNoTracking()
            .Where(a => a.WorkItemId == id)
            .OrderByDescending(a => a.AcceptanceDate)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> AddAcceptedAsync(
        int id, AcceptedQuantityCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var w = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return Results.NotFound(ApiResponse<object>.Fail("Đầu mục không tồn tại"));

        var userId = principal.GetUserId();
        var a = new AcceptedQuantity
        {
            WorkItemId = id,
            AcceptanceDate = dto.AcceptanceDate,
            AcceptedQuantityValue = dto.AcceptedQuantityValue,
            AcceptanceMinutes = dto.AcceptanceMinutes,
            Inspector = dto.Inspector,
            Notes = dto.Notes,
            CreatedBy = userId
        };
        db.AcceptedQuantities.Add(a);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "AcceptedQuantity", a.Id, null, a,
            "Thêm khối lượng nghiệm thu", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { a.Id }));
    }

    private static async Task<IResult> DeleteAcceptedAsync(
        int id, int accId, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var a = await db.AcceptedQuantities.FirstOrDefaultAsync(x => x.WorkItemId == id && x.Id == accId, ct);
        if (a is null) return Results.NotFound();
        db.AcceptedQuantities.Remove(a);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "AcceptedQuantity", accId, a, null,
            "Xóa khối lượng nghiệm thu", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { accId }, "Đã xóa"));
    }

    // ============ Cost Reports (Module 3.4 + Module 6.1) ============

    private static async Task<IResult> ProfitByWorkItemAsync(int workItemId, ServerBMCDbContext db, CancellationToken ct)
    {
        var w = await db.WorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workItemId, ct);
        if (w is null) return Results.NotFound();

        var dto = await ComputeWorkItemProfitAsync(db, workItemId, w, ct);
        return Results.Ok(ApiResponse<WorkItemProfitDto>.Ok(dto));
    }

    private static async Task<WorkItemProfitDto> ComputeWorkItemProfitAsync(
        ServerBMCDbContext db, int id, WorkItem w, CancellationToken ct)
    {
        var accepted = await db.AcceptedQuantities.Where(a => a.WorkItemId == id)
            .SumAsync(a => (decimal?)a.AcceptedQuantityValue, ct) ?? 0m;
        var bidPrice = await db.UnitPrices.Where(u => u.WorkItemId == id && u.PriceType == "VL")
            .OrderByDescending(u => u.EffectiveFrom)
            .Select(u => (decimal?)u.UnitPriceValue).FirstOrDefaultAsync(ct) ?? 0m;
        var actual = await db.ActualCosts.Where(c => c.WorkItemId == id)
            .SumAsync(c => (decimal?)c.TotalAmount, ct) ?? 0m;

        var revenue = accepted * bidPrice;
        var profit = revenue - actual;
        var pct = revenue > 0 ? actual / revenue * 100m : 0m;
        string level = pct >= 100m ? "Red" : pct >= 90m ? "Yellow" : "None";

        return new WorkItemProfitDto(id, w.ItemCode, w.ItemName, w.Unit,
            accepted, bidPrice, revenue, actual, profit, pct, level);
    }

    private static async Task<IResult> ProfitByCategoryAsync(int categoryId, ServerBMCDbContext db, CancellationToken ct)
    {
        var ids = await db.WorkItems.Where(w => w.SubCategory.CategoryId == categoryId)
            .Select(w => w.Id).ToListAsync(ct);
        var sum = await AggregateProfitAsync(db, ids, ct);
        return Results.Ok(ApiResponse<ProfitSummaryDto>.Ok(sum with { CategoryId = categoryId }));
    }

    private static async Task<IResult> ProfitByLotAsync(int lotId, ServerBMCDbContext db, CancellationToken ct)
    {
        var ids = await db.WorkItems.Where(w => w.SubCategory.Category.ProjectLotId == lotId)
            .Select(w => w.Id).ToListAsync(ct);
        var sum = await AggregateProfitAsync(db, ids, ct);
        return Results.Ok(ApiResponse<ProfitSummaryDto>.Ok(sum with { LotId = lotId }));
    }

    private static async Task<IResult> ProfitByProjectAsync(int projectId, ServerBMCDbContext db, CancellationToken ct)
    {
        var ids = await db.WorkItems.Where(w => w.SubCategory.Category.ProjectLot.ProjectId == projectId)
            .Select(w => w.Id).ToListAsync(ct);
        var sum = await AggregateProfitAsync(db, ids, ct);
        return Results.Ok(ApiResponse<ProfitSummaryDto>.Ok(sum with { ProjectId = projectId }));
    }

    private static async Task<ProfitSummaryDto> AggregateProfitAsync(
        ServerBMCDbContext db, List<int> workItemIds, CancellationToken ct)
    {
        if (workItemIds.Count == 0) return new ProfitSummaryDto(null, null, null, 0, 0, 0);
        var accepted = await db.AcceptedQuantities.Where(a => workItemIds.Contains(a.WorkItemId))
            .SumAsync(a => (decimal?)a.AcceptedQuantityValue, ct) ?? 0m;
        var bidRev = await db.UnitPrices
            .Where(u => workItemIds.Contains(u.WorkItemId) && u.PriceType == "VL")
            .GroupBy(_ => 1)
            .Select(g => g.Sum(x => x.UnitPriceValue)).SumAsync(ct); // sum-of-price (sẽ chuẩn hoá với accepted)
        var actual = await db.ActualCosts.Where(c => workItemIds.Contains(c.WorkItemId))
            .SumAsync(c => (decimal?)c.TotalAmount, ct) ?? 0m;

        // Chuẩn hoá: revenue = accepted * average bid price (đơn giá VL)
        var avgBid = await db.UnitPrices.Where(u => workItemIds.Contains(u.WorkItemId) && u.PriceType == "VL")
            .AverageAsync(u => (decimal?)u.UnitPriceValue, ct) ?? 0m;
        var revenue = accepted * avgBid;
        return new ProfitSummaryDto(null, null, null, revenue, actual, revenue - actual);
    }

    private static async Task<IResult> CategoryCostCompareAsync(int categoryId, ServerBMCDbContext db, CancellationToken ct)
    {
        var workItemIds = await db.WorkItems.Where(w => w.SubCategory.CategoryId == categoryId)
            .Select(w => w.Id).ToListAsync(ct);
        if (workItemIds.Count == 0)
            return Results.Ok(ApiResponse<CategoryCostCompareDto>.Ok(new CategoryCostCompareDto(
                categoryId, "", 0, 0, 0, 0, 0, 0, 0, 0, 0)));

        var cat = await db.Categories.AsNoTracking().FirstAsync(c => c.Id == categoryId, ct);

        var accepted = await db.AcceptedQuantities.Where(a => workItemIds.Contains(a.WorkItemId))
            .SumAsync(a => (decimal?)a.AcceptedQuantityValue, ct) ?? 0m;

        // Bid unit prices by type (VL/NC/May)
        var bidByType = await db.UnitPrices.Where(u => workItemIds.Contains(u.WorkItemId))
            .GroupBy(u => u.PriceType)
            .Select(g => new { Type = g.Key, Avg = g.Average(x => x.UnitPriceValue) })
            .ToListAsync(ct);
        decimal bidVl = bidByType.FirstOrDefault(x => x.Type == "VL")?.Avg ?? 0m;
        decimal bidNc = bidByType.FirstOrDefault(x => x.Type == "NC")?.Avg ?? 0m;
        decimal bidMay = bidByType.FirstOrDefault(x => x.Type == "May")?.Avg ?? 0m;

        // Actual by type
        var actByType = await db.ActualCosts.Where(c => workItemIds.Contains(c.WorkItemId))
            .GroupBy(c => c.CostType)
            .Select(g => new { Type = g.Key, Sum = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);
        decimal actVl = actByType.FirstOrDefault(x => x.Type == "VL")?.Sum ?? 0m;
        decimal actNc = actByType.FirstOrDefault(x => x.Type == "NC")?.Sum ?? 0m;
        decimal actMay = actByType.FirstOrDefault(x => x.Type == "May")?.Sum ?? 0m;
        decimal actKhac = actByType.FirstOrDefault(x => x.Type == "Khac")?.Sum ?? 0m;

        // Bid revenue = KL nghiệm thu × đơn giá VL dự thầu (theo tài liệu)
        var bidRevenue = accepted * bidVl;
        var profit = bidRevenue - (actVl + actNc + actMay + actKhac);

        var dto = new CategoryCostCompareDto(
            categoryId, cat.CategoryName,
            bidRevenue,
            accepted * bidVl, accepted * bidNc, accepted * bidMay,
            actVl, actNc, actMay, actKhac,
            profit);

        return Results.Ok(ApiResponse<CategoryCostCompareDto>.Ok(dto));
    }
}