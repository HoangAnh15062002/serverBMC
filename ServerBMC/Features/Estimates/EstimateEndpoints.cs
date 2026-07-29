using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Estimates;

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
        g.MapGet("/{id:int}/work-items", GetWorkItemsAsync);
        g.MapGet("/{estimateId:int}/work-items/{workItemId:int}/details", GetWorkItemDetailsAsync);
        g.MapPut("/{estimateId:int}/work-items/{workItemId}/details/{detailId:int}", UpdateWorkItemDetailAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));
        g.MapPut("/{estimateId:int}/work-items/{workItemId}", UpdateWorkItemAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] PagedRequest p,
        string? search,
        ServerBMCDbContext db,
        CancellationToken ct)
    {
        var query = db.Estimates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.ProjectName.Contains(s) || x.Category.Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(p.Skip).Take(p.Take)
            .Select(x => new
            {
                x.Id,
                x.ProjectName,
                x.Category,
                x.Location,
                x.Investor,
                x.Scope,
                x.TotalAmount,
                x.DocumentType,
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

    private static async Task<IResult> GetAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var estimate = await db.Estimates.AsNoTracking()
            .Include(x => x.WorkItems).ThenInclude(w => w.Details)
            .Include(x => x.CostSummary)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (estimate == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy dự toán"));

        var dto = MapToDto(estimate);
        return Results.Ok(ApiResponse<object>.Ok(dto));
    }

    private static async Task<IResult> CreateAsync(
        CreateEstimateDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var estimate = new Estimate
        {
            ProjectName = dto.ProjectName,
            Category = dto.Category,
            Location = dto.Location,
            Investor = dto.Investor,
            Consultant = dto.Consultant,
            Scope = dto.Scope,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            CreatedBy = userId
        };

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);

        // Add work items if provided
        if (dto.WorkItems.Any())
        {
            foreach (var wiDto in dto.WorkItems)
            {
                var workItem = new EstimateWorkItem
                {
                    EstimateId = estimate.Id,
                    Stt = wiDto.Stt,
                    Code = wiDto.Code,
                    Name = wiDto.Name,
                    Unit = wiDto.Unit,
                    Quantity = wiDto.Quantity
                };

                db.EstimateWorkItems.Add(workItem);
                await db.SaveChangesAsync(ct);

                foreach (var detDto in wiDto.Details)
                {
                    var detail = new WorkItemDetail
                    {
                        WorkItemId = workItem.Id,
                        Category = detDto.Category,
                        Code = detDto.Code,
                        Name = detDto.Name,
                        Unit = detDto.Unit,
                        Quantity = detDto.Quantity,
                        UnitPrice = detDto.UnitPrice,
                        Factor = detDto.Factor,
                        TotalAmount = detDto.Quantity * detDto.UnitPrice * detDto.Factor
                    };
                    db.WorkItemDetails.Add(detail);
                }
                await db.SaveChangesAsync(ct);

                // Calculate work item totals
                CalculateWorkItemTotals(workItem, db);
            }
        }

        // Calculate cost summary
        CalculateCostSummary(estimate.Id, db);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(db, userId, "Create", "Estimate", estimate.Id, null, dto,
            "Tạo dự toán", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { estimate.Id }, "Tạo dự toán thành công"));
    }

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

        var old = new { estimate.ProjectName, estimate.Category, estimate.Location, estimate.Investor };

        estimate.ProjectName = dto.ProjectName;
        estimate.Category = dto.Category;
        estimate.Location = dto.Location;
        estimate.Investor = dto.Investor;
        estimate.Consultant = dto.Consultant;
        estimate.Scope = dto.Scope;
        estimate.DocumentNumber = dto.DocumentNumber;
        estimate.DocumentDate = dto.DocumentDate;
        estimate.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "Estimate", id, old, dto,
            "Cập nhật dự toán", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { id }, "Cập nhật thành công"));
    }

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

    private static async Task<IResult> GetCostSummaryAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var summary = await db.CostSummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EstimateId == id, ct);

        if (summary == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy bảng tổng hợp kinh phí"));

        return Results.Ok(ApiResponse<object>.Ok(summary));
    }

    private static async Task<IResult> GetWorkItemsAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.EstimateWorkItems.AsNoTracking()
            .Where(x => x.EstimateId == id)
            .OrderBy(x => x.Stt)
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> GetWorkItemDetailsAsync(
        int estimateId,
        int workItemId,
        ServerBMCDbContext db,
        CancellationToken ct)
    {
        var details = await db.WorkItemDetails.AsNoTracking()
            .Where(x => x.WorkItemId == workItemId)
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(details));
    }

    private static async Task<IResult> UpdateWorkItemDetailAsync(
        int estimateId,
        int workItemId,
        int detailId,
        UpdateWorkItemDetailDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var detail = await db.WorkItemDetails.FirstOrDefaultAsync(
            x => x.Id == detailId && x.WorkItemId == workItemId, ct);

        if (detail == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy chi tiết"));

        // Update fields
        detail.Quantity = dto.Quantity;
        detail.UnitPrice = dto.UnitPrice;
        detail.Factor = dto.Factor;
        detail.TotalAmount = dto.Quantity * dto.UnitPrice * dto.Factor;

        // Recalculate WorkItem totals
        var workItem = await db.EstimateWorkItems.FindAsync(new object[] { workItemId }, ct);
        if (workItem != null)
        {
            CalculateWorkItemTotals(workItem, db);
            await db.SaveChangesAsync(ct);

            // Recalculate Estimate summary
            CalculateCostSummary(estimateId, db);
            var estimate = await db.Estimates.FindAsync(new object[] { estimateId }, ct);
            if (estimate != null)
            {
                estimate.TotalAmount = workItem.TotalAmount;
                estimate.TotalAmountText = ConvertToVietnameseText(estimate.TotalAmount);
            }
        }

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "WorkItemDetail", detailId,
            null, dto, "Cập nhật chi tiết định mức", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);

        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { detailId }, "Cập nhật thành công"));
    }

    private static async Task<IResult> UpdateWorkItemAsync(
        int estimateId,
        int workItemId,
        UpdateWorkItemDto dto,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        var workItem = await db.EstimateWorkItems.FirstOrDefaultAsync(
            x => x.Id == workItemId && x.EstimateId == estimateId, ct);

        if (workItem == null)
            return Results.NotFound(ApiResponse<object>.Fail("Không tìm thấy hạng mục"));

        // Update quantity
        workItem.Quantity = dto.Quantity;

        // Recalculate unit prices (keep totals, just recalculate unit prices)
        if (workItem.Quantity > 0)
        {
            workItem.MaterialUnitPrice = workItem.MaterialTotal / workItem.Quantity;
            workItem.LaborUnitPrice = workItem.LaborTotal / workItem.Quantity;
            workItem.MachineUnitPrice = workItem.MachineTotal / workItem.Quantity;
        }

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "EstimateWorkItem", workItemId,
            null, dto, "Cập nhật hạng mục", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);

        await db.SaveChangesAsync(ct);

        // Recalculate Estimate summary
        CalculateCostSummary(estimateId, db);
        var estimate = await db.Estimates.FindAsync(new object[] { estimateId }, ct);
        if (estimate != null)
        {
            var allWorkItems = await db.EstimateWorkItems.Where(x => x.EstimateId == estimateId).ToListAsync(ct);
            estimate.TotalAmount = allWorkItems.Sum(x => x.TotalAmount);
            estimate.TotalAmountText = ConvertToVietnameseText(estimate.TotalAmount);
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(ApiResponse<object>.Ok(new { workItemId }, "Cập nhật thành công"));
    }

    private static void CalculateWorkItemTotals(EstimateWorkItem workItem, ServerBMCDbContext db)
    {
        var details = db.WorkItemDetails.Where(x => x.WorkItemId == workItem.Id).ToList();

        workItem.MaterialTotal = details.Where(x => x.Category == "Vật liệu").Sum(x => x.TotalAmount);
        workItem.LaborTotal = details.Where(x => x.Category == "Nhân công").Sum(x => x.TotalAmount);
        workItem.MachineTotal = details.Where(x => x.Category == "Máy").Sum(x => x.TotalAmount);
        workItem.TotalAmount = workItem.MaterialTotal + workItem.LaborTotal + workItem.MachineTotal;

        // Update unit prices
        if (workItem.Quantity > 0)
        {
            workItem.MaterialUnitPrice = workItem.MaterialTotal / workItem.Quantity;
            workItem.LaborUnitPrice = workItem.LaborTotal / workItem.Quantity;
            workItem.MachineUnitPrice = workItem.MachineTotal / workItem.Quantity;
        }
    }

    private static void CalculateCostSummary(int estimateId, ServerBMCDbContext db)
    {
        var workItems = db.EstimateWorkItems.Where(x => x.EstimateId == estimateId).ToList();

        var summary = db.CostSummaries.FirstOrDefault(x => x.EstimateId == estimateId)
                      ?? new CostSummary { EstimateId = estimateId };

        // I. Chi phí trực tiếp
        summary.MaterialCost = workItems.Sum(x => x.MaterialTotal);
        summary.LaborCost = workItems.Sum(x => x.LaborTotal);
        summary.MachineCost = workItems.Sum(x => x.MachineTotal);
        summary.DirectCost = summary.MaterialCost + summary.LaborCost + summary.MachineCost;

        // II. Chi phí gián tiếp
        summary.GeneralCost = Math.Round(summary.DirectCost * summary.GeneralCostRate);
        summary.OverheadCost = Math.Round(summary.DirectCost * summary.OverheadCostRate);
        summary.UndeterminedCost = Math.Round(summary.DirectCost * summary.UndeterminedCostRate);
        summary.IndirectCost = summary.GeneralCost + summary.OverheadCost + summary.UndeterminedCost;

        // III. Thu nhập chịu thuế
        summary.PreTaxIncome = Math.Round((summary.DirectCost + summary.IndirectCost) * summary.PreTaxIncomeRate);
        summary.PreTaxAmount = summary.DirectCost + summary.IndirectCost + summary.PreTaxIncome;

        // IV. Thuế GTGT
        summary.VatAmount = Math.Round(summary.PreTaxAmount * summary.VatRate);

        // V. Tổng cộng
        summary.PostTaxAmount = summary.PreTaxAmount + summary.VatAmount;
        summary.RoundedAmount = Math.Round(summary.PostTaxAmount);

        // Update estimate total
        var estimate = db.Estimates.Find(estimateId);
        if (estimate != null)
        {
            estimate.TotalAmount = summary.RoundedAmount;
            estimate.TotalAmountText = NumberToText(summary.RoundedAmount);
        }

        if (summary.Id == 0)
            db.CostSummaries.Add(summary);
    }

    private static string NumberToText(decimal number)
    {
        if (number == 0) return "Không đồng";

        var units = new[] { "", "nghìn", "triệu", "tỷ" };
        var result = "";

        var numStr = ((long)number).ToString("N0");
        var parts = numStr.Split(',');

        for (int i = 0; i < parts.Length; i++)
        {
            var partValue = long.Parse(parts[i]);
            var unitIndex = parts.Length - i - 1;
            if (partValue > 0)
            {
                result += $"{partValue:N0} {units[unitIndex]} ";
            }
        }

        return result.Trim() + " đồng";
    }

    private static EstimateDto MapToDto(Estimate e)
    {
        return new EstimateDto
        {
            Id = e.Id,
            ProjectName = e.ProjectName,
            Category = e.Category,
            Location = e.Location,
            Investor = e.Investor,
            Consultant = e.Consultant,
            Scope = e.Scope,
            DocumentType = e.DocumentType,
            DocumentNumber = e.DocumentNumber,
            DocumentDate = e.DocumentDate,
            TotalAmount = e.TotalAmount,
            TotalAmountText = e.TotalAmountText,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            WorkItems = e.WorkItems.Select(w => new EstimateWorkItemDto
            {
                Id = w.Id,
                Stt = w.Stt,
                Code = w.Code,
                Name = w.Name,
                Unit = w.Unit,
                Quantity = w.Quantity,
                MaterialUnitPrice = w.MaterialUnitPrice,
                LaborUnitPrice = w.LaborUnitPrice,
                MachineUnitPrice = w.MachineUnitPrice,
                MaterialTotal = w.MaterialTotal,
                LaborTotal = w.LaborTotal,
                MachineTotal = w.MachineTotal,
                TotalAmount = w.TotalAmount,
                Details = w.Details.Select(d => new WorkItemDetailDto
                {
                    Id = d.Id,
                    Category = d.Category,
                    Code = d.Code,
                    Name = d.Name,
                    Unit = d.Unit,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Factor = d.Factor,
                    TotalAmount = d.TotalAmount
                }).ToList()
            }).ToList(),
            CostSummary = e.CostSummary != null ? new CostSummaryDto
            {
                Id = e.CostSummary.Id,
                MaterialCost = e.CostSummary.MaterialCost,
                LaborCost = e.CostSummary.LaborCost,
                MachineCost = e.CostSummary.MachineCost,
                DirectCost = e.CostSummary.DirectCost,
                GeneralCost = e.CostSummary.GeneralCost,
                GeneralCostRate = e.CostSummary.GeneralCostRate,
                OverheadCost = e.CostSummary.OverheadCost,
                OverheadCostRate = e.CostSummary.OverheadCostRate,
                UndeterminedCost = e.CostSummary.UndeterminedCost,
                UndeterminedCostRate = e.CostSummary.UndeterminedCostRate,
                IndirectCost = e.CostSummary.IndirectCost,
                PreTaxIncome = e.CostSummary.PreTaxIncome,
                PreTaxIncomeRate = e.CostSummary.PreTaxIncomeRate,
                PreTaxAmount = e.CostSummary.PreTaxAmount,
                VatAmount = e.CostSummary.VatAmount,
                VatRate = e.CostSummary.VatRate,
                PostTaxAmount = e.CostSummary.PostTaxAmount,
                RoundedAmount = e.CostSummary.RoundedAmount
            } : null
        };
    }

    private static string ConvertToVietnameseText(decimal amount)
    {
        if (amount == 0) return "Không đồng";

        var number = (long)amount;
        var str = number.ToString();
        var result = "";

        var units = new[] { "", "nghìn", "triệu", "tỷ" };
        var digits = new[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

        if (str.Length <= 3)
        {
            result = Read3Digits(str, digits);
        }
        else
        {
            var groups = new List<string>();
            var temp = str;
            while (temp.Length > 0)
            {
                groups.Insert(0, temp.Substring(Math.Max(0, temp.Length - 3)));
                temp = temp.Substring(0, Math.Max(0, temp.Length - 3));
            }

            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                if (g != "000")
                {
                    var val = Read3Digits(g, digits);
                    result += val + " " + units[groups.Count - i - 1] + " ";
                }
            }
        }

        result = result.Trim();
        if (result.EndsWith(",")) result = result.Substring(0, result.Length - 1);

        // Xử lý phần thập phân
        var decimalPart = amount - number;
        if (decimalPart > 0)
        {
            var decimalStr = ((int)(decimalPart * 1000)).ToString().TrimEnd('0');
            if (!string.IsNullOrEmpty(decimalStr))
            {
                result += " " + decimalStr + " đồng";
            }
        }
        else
        {
            result += " đồng";
        }

        // Viết hoa chữ cái đầu
        if (result.Length > 0)
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }

        return result;
    }

    private static string Read3Digits(string s, string[] digits)
    {
        if (s.Length != 3) s = s.PadLeft(3, '0');

        var result = "";
        var a = int.Parse(s[0].ToString());
        var b = int.Parse(s[1].ToString());
        var c = int.Parse(s[2].ToString());

        if (a > 0) result += digits[a] + " trăm";
        else if (b > 0 || c > 0) result += "không trăm";

        if (b > 0) result += " " + (b == 1 ? "mười" : digits[b] + " mươi");
        else if (c > 0) result += " linh";

        if (b == 0 && c > 0)
        {
            result += " " + digits[c];
        }
        else if (b == 1 && c > 0)
        {
            result = result.TrimEnd(' ') + " " + digits[c];
        }
        else if (b > 1 && c > 0)
        {
            result += " " + digits[c];
        }

        return result.Trim();
    }
}
