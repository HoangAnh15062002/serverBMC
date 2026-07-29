using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Domain.Entities;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Services;

public class ExcelImportService
{
    private readonly ServerBMCDbContext _db;

    public ExcelImportService(ServerBMCDbContext db)
    {
        _db = db;
    }

    public async Task<int> ImportFromExcel(string filePath, CancellationToken ct = default)
    {
        var excel = new Microsoft.Office.Interop.Excel.Application();
        excel.Visible = false;
        excel.DisplayAlerts = false;

        try
        {
            var wb = excel.Workbooks.Open(filePath);
            
            // Import from "Mong M02B" file (56 sheets)
            var sheetIndex = 1;
            var ws = wb.Sheets[sheetIndex]; // THKP hạng mục
            
            var estimate = await ImportCostSummary(ws, ct);
            
            // Import "Giá tổng hợp" sheet
            ws = wb.Sheets[2]; // Sheet 2: Giá tổng hợp
            await ImportWorkItems(ws, estimate.Id, ct);

            // Import "Đơn giá chi tiết" sheet
            ws = wb.Sheets[3]; // Sheet 3: Đơn giá chi tiết
            await ImportWorkItemDetails(ws, estimate.Id, ct);

            wb.Close(false);
            
            // Calculate totals
            await CalculateTotals(estimate.Id, ct);
            
            return estimate.Id;
        }
        finally
        {
            excel.Quit();
            Marshal.ReleaseComObject(excel);
        }
    }

    private async Task<Estimate> ImportCostSummary(dynamic ws, CancellationToken ct)
    {
        // Read header info from sheet
        string projectName = "", category = "", location = "", investor = "", consultant = "";

        try { projectName = GetCellValue(ws, 3, 4); } catch { }
        try { category = GetCellValue(ws, 3, 1); } catch { }
        
        // Remove "DỰ ÁN: " prefix if exists
        if (projectName.StartsWith("DỰ ÁN: "))
            projectName = projectName.Substring(7);
        if (projectName.StartsWith("D? AN: "))
            projectName = projectName.Substring(7);

        var estimate = new Estimate
        {
            ProjectName = projectName,
            Category = ExtractCategory(category),
            Location = location,
            Investor = investor,
            Consultant = consultant,
            DocumentType = "M-02B"
        };

        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync(ct);

        // Read cost summary from rows 7-25
        var costSummary = new CostSummary
        {
            EstimateId = estimate.Id
        };

        // Row 8: Chi phí Vật liệu
        try { costSummary.MaterialCost = ParseDecimal(GetCellValue(ws, 8, 8)); } catch { }
        
        // Row 10: Chi phí Nhân công
        try { costSummary.LaborCost = ParseDecimal(GetCellValue(ws, 10, 8)); } catch { }
        
        // Row 12: Chi phí Máy
        try { costSummary.MachineCost = ParseDecimal(GetCellValue(ws, 12, 8)); } catch { }
        
        // Row 14: Tổng chi phí trực tiếp
        try { costSummary.DirectCost = ParseDecimal(GetCellValue(ws, 14, 8)); } catch { }
        
        // Row 16: Chi phí chung
        try { costSummary.GeneralCostRate = ParseDecimal(GetCellValue(ws, 16, 7)) / 100; } catch { }
        try { costSummary.GeneralCost = ParseDecimal(GetCellValue(ws, 16, 8)); } catch { }
        
        // Row 17: Chi phí nhà tạm
        try { costSummary.OverheadCostRate = ParseDecimal(GetCellValue(ws, 17, 7)) / 100; } catch { }
        try { costSummary.OverheadCost = ParseDecimal(GetCellValue(ws, 17, 8)); } catch { }
        
        // Row 18: Chi phí không xác định
        try { costSummary.UndeterminedCostRate = ParseDecimal(GetCellValue(ws, 18, 7)) / 100; } catch { }
        try { costSummary.UndeterminedCost = ParseDecimal(GetCellValue(ws, 18, 8)); } catch { }
        
        // Row 19: Tổng chi phí gián tiếp
        try { costSummary.IndirectCost = ParseDecimal(GetCellValue(ws, 19, 8)); } catch { }
        
        // Row 20: Thu nhập chịu thuế
        try { costSummary.PreTaxIncomeRate = ParseDecimal(GetCellValue(ws, 20, 7)) / 100; } catch { }
        try { costSummary.PreTaxIncome = ParseDecimal(GetCellValue(ws, 20, 8)); } catch { }
        
        // Row 21: Chi phí trước thuế
        try { costSummary.PreTaxAmount = ParseDecimal(GetCellValue(ws, 21, 8)); } catch { }
        
        // Row 22: Thuế GTGT
        try { costSummary.VatRate = ParseDecimal(GetCellValue(ws, 22, 7)) / 100; } catch { }
        try { costSummary.VatAmount = ParseDecimal(GetCellValue(ws, 22, 8)); } catch { }
        
        // Row 23: Chi phí sau thuế
        try { costSummary.PostTaxAmount = ParseDecimal(GetCellValue(ws, 23, 8)); } catch { }
        
        // Row 24: Làm tròn
        try { costSummary.RoundedAmount = ParseDecimal(GetCellValue(ws, 24, 8)); } catch { }

        _db.CostSummaries.Add(costSummary);
        await _db.SaveChangesAsync(ct);

        estimate.TotalAmount = costSummary.RoundedAmount;
        estimate.TotalAmountText = NumberToText(costSummary.RoundedAmount);
        await _db.SaveChangesAsync(ct);

        return estimate;
    }

    private async Task ImportWorkItems(dynamic ws, int estimateId, CancellationToken ct)
    {
        var usedRange = ws.UsedRange;
        int rowCount = usedRange.Rows.Count;

        int stt = 0;
        for (int r = 7; r <= rowCount; r++) // Start from row 7 (data starts there)
        {
            try
            {
                var sttValue = GetCellValue(ws, r, 1);
                if (string.IsNullOrWhiteSpace(sttValue)) continue;
                
                // Skip section headers
                if (!int.TryParse(sttValue, out int sttNum)) continue;

                var workItem = new EstimateWorkItem
                {
                    EstimateId = estimateId,
                    Stt = sttNum,
                    Code = GetCellValue(ws, r, 2),
                    Name = GetCellValue(ws, r, 3),
                    Unit = GetCellValue(ws, r, 4),
                    Quantity = ParseDecimal(GetCellValue(ws, r, 5)),
                    MaterialUnitPrice = ParseDecimal(GetCellValue(ws, r, 6)),
                    LaborUnitPrice = ParseDecimal(GetCellValue(ws, r, 7)),
                    MachineUnitPrice = ParseDecimal(GetCellValue(ws, r, 8)),
                    MaterialTotal = ParseDecimal(GetCellValue(ws, r, 9)),
                    LaborTotal = ParseDecimal(GetCellValue(ws, r, 10)),
                    MachineTotal = ParseDecimal(GetCellValue(ws, r, 11)),
                    TotalAmount = ParseDecimal(GetCellValue(ws, r, 13))
                };

                _db.EstimateWorkItems.Add(workItem);
                await _db.SaveChangesAsync(ct);
                stt++;
            }
            catch { /* Skip invalid rows */ }
        }
    }

    private async Task ImportWorkItemDetails(dynamic ws, int estimateId, CancellationToken ct)
    {
        // Get all work items for this estimate
        var workItems = await _db.EstimateWorkItems
            .Where(x => x.EstimateId == estimateId)
            .ToListAsync(ct);

        var usedRange = ws.UsedRange;
        int rowCount = usedRange.Rows.Count;

        int? currentWorkItemId = null;
        int sttNum = 0;
        for (int r = 6; r <= rowCount; r++)
        {
            try
            {
                var sttValue = GetCellValue(ws, r, 1);
                var codeValue = GetCellValue(ws, r, 2);
                
                // Check if this is a new work item
                if (!string.IsNullOrWhiteSpace(sttValue) && int.TryParse(sttValue, out sttNum))
                {
                    var wi = workItems.FirstOrDefault(x => x.Stt == sttNum);
                    if (wi != null)
                        currentWorkItemId = wi.Id;
                }

                if (currentWorkItemId == null) continue;

                // Check category (a, b, c in column 3)
                var category = GetCellValue(ws, r, 3);
                if (string.IsNullOrWhiteSpace(category)) continue;
                
                // Skip subtotal rows
                if (category.Contains("Cộng") || category.Contains("Tổng")) continue;

                string catType;
                if (category.StartsWith("a)") || category.Contains("Vật liệu") || category.Contains("VL"))
                    catType = "Vật liệu";
                else if (category.StartsWith("b)") || category.Contains("Nhân công") || category.Contains("NC"))
                    catType = "Nhân công";
                else if (category.StartsWith("c)") || category.Contains("Máy") || category.Contains("M"))
                    catType = "Máy";
                else
                    continue;

                var detail = new WorkItemDetail
                {
                    WorkItemId = currentWorkItemId.Value,
                    Category = catType,
                    Code = GetCellValue(ws, r, 2),
                    Name = category,
                    Unit = GetCellValue(ws, r, 4),
                    Quantity = ParseDecimal(GetCellValue(ws, r, 5)),
                    UnitPrice = ParseDecimal(GetCellValue(ws, r, 6)),
                    Factor = ParseDecimal(GetCellValue(ws, r, 7)),
                    TotalAmount = ParseDecimal(GetCellValue(ws, r, 8))
                };

                if (detail.Quantity > 0 || detail.TotalAmount > 0)
                {
                    _db.WorkItemDetails.Add(detail);
                    await _db.SaveChangesAsync(ct);
                }
            }
            catch { /* Skip invalid rows */ }
        }
    }

    private async Task CalculateTotals(int estimateId, CancellationToken ct)
    {
        var workItems = await _db.EstimateWorkItems
            .Where(x => x.EstimateId == estimateId)
            .ToListAsync(ct);

        foreach (var wi in workItems)
        {
            var details = await _db.WorkItemDetails
                .Where(x => x.WorkItemId == wi.Id)
                .ToListAsync(ct);

            wi.MaterialTotal = details.Where(x => x.Category == "Vật liệu").Sum(x => x.TotalAmount);
            wi.LaborTotal = details.Where(x => x.Category == "Nhân công").Sum(x => x.TotalAmount);
            wi.MachineTotal = details.Where(x => x.Category == "Máy").Sum(x => x.TotalAmount);
            wi.TotalAmount = wi.MaterialTotal + wi.LaborTotal + wi.MachineTotal;

            if (wi.Quantity > 0)
            {
                wi.MaterialUnitPrice = wi.MaterialTotal / wi.Quantity;
                wi.LaborUnitPrice = wi.LaborTotal / wi.Quantity;
                wi.MachineUnitPrice = wi.MachineTotal / wi.Quantity;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Recalculate cost summary
        var summary = await _db.CostSummaries
            .FirstOrDefaultAsync(x => x.EstimateId == estimateId, ct);

        if (summary != null)
        {
            summary.MaterialCost = workItems.Sum(x => x.MaterialTotal);
            summary.LaborCost = workItems.Sum(x => x.LaborTotal);
            summary.MachineCost = workItems.Sum(x => x.MachineTotal);
            summary.DirectCost = summary.MaterialCost + summary.LaborCost + summary.MachineCost;

            summary.GeneralCost = Math.Round(summary.DirectCost * summary.GeneralCostRate);
            summary.OverheadCost = Math.Round(summary.DirectCost * summary.OverheadCostRate);
            summary.UndeterminedCost = Math.Round(summary.DirectCost * summary.UndeterminedCostRate);
            summary.IndirectCost = summary.GeneralCost + summary.OverheadCost + summary.UndeterminedCost;

            summary.PreTaxIncome = Math.Round((summary.DirectCost + summary.IndirectCost) * summary.PreTaxIncomeRate);
            summary.PreTaxAmount = summary.DirectCost + summary.IndirectCost + summary.PreTaxIncome;
            summary.VatAmount = Math.Round(summary.PreTaxAmount * summary.VatRate);
            summary.PostTaxAmount = summary.PreTaxAmount + summary.VatAmount;
            summary.RoundedAmount = Math.Round(summary.PostTaxAmount);

            await _db.SaveChangesAsync(ct);

            var estimate = await _db.Estimates.FindAsync(new object[] { estimateId }, ct);
            if (estimate != null)
            {
                estimate.TotalAmount = summary.RoundedAmount;
                estimate.TotalAmountText = NumberToText(summary.RoundedAmount);
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private static string GetCellValue(dynamic ws, int row, int col)
    {
        try
        {
            var val = ws.Cells[row, col].Text;
            return val?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        
        // Remove thousand separators and whitespace
        value = value.Replace(",", "").Replace(" ", "").Trim();
        
        // Handle Vietnamese number format (dot as thousand separator)
        value = value.Replace(".", "");

        if (decimal.TryParse(value, out decimal result))
            return result;
        
        return 0;
    }

    private static string ExtractCategory(string category)
    {
        // Extract "MÓNG" from "HẠNG MỤC: XÂY THÔ VÀ HOÀN THIỆN MẶT NGOÀI (LO BT-C1) - PHẦN: MÓNG"
        if (category.Contains(":"))
        {
            var parts = category.Split(':');
            if (parts.Length > 1)
            {
                var scope = parts[1].Trim();
                if (scope.Contains("-"))
                {
                    var scopeParts = scope.Split('-');
                    if (scopeParts.Length > 1)
                        return scopeParts[1].Trim();
                }
                return scope;
            }
        }
        return category;
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
}
