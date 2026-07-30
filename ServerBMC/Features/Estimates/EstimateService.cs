using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Estimates;

/// <summary>
/// Single source of truth cho tất cả logic nghiệp vụ của Estimate:
/// - Tạo Estimate mới (từ DTO)
/// - Tính lại totals cho 1 EstimateItem
/// - Tính lại CostSummary + Estimate.TotalAmount
/// - Convert số → chữ tiếng Việt (1 hàm duy nhất)
/// - Import dữ liệu từ Excel (Excel COM - chỉ chạy trên Windows)
/// </summary>
public class EstimateService
{
    private const string CAT_MATERIAL = "Vật liệu";
    private const string CAT_LABOR = "Nhân công";
    private const string CAT_MACHINE = "Máy";

    private readonly ServerBMCDbContext _db;

    public EstimateService(ServerBMCDbContext db)
    {
        _db = db;
    }

    // ====================================================================
    // CREATE
    // ====================================================================

    public async Task<Estimate> CreateAsync(CreateEstimateDto dto, int? userId, CancellationToken ct)
    {
        var estimate = new Estimate
        {
            EstimateCategoryId = dto.EstimateCategoryId,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            CreatedBy = userId
        };

        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync(ct);

        if (dto.Items.Any())
        {
            foreach (var itemDto in dto.Items)
            {
                var item = new EstimateItem
                {
                    EstimateId = estimate.Id,
                    Stt = itemDto.Stt,
                    Code = itemDto.Code,
                    Name = itemDto.Name,
                    Unit = itemDto.Unit,
                    Quantity = itemDto.Quantity
                };
                _db.EstimateItems.Add(item);
                await _db.SaveChangesAsync(ct);

                foreach (var detDto in itemDto.Details)
                {
                    var detail = new EstimateItemDetail
                    {
                        EstimateItemId = item.Id,
                        DetailType = detDto.Category,
                        Code = detDto.Code,
                        Name = detDto.Name,
                        Unit = detDto.Unit,
                        Quantity = detDto.Quantity,
                        UnitPrice = detDto.UnitPrice,
                        Factor = detDto.Factor,
                        TotalAmount = detDto.Quantity * detDto.UnitPrice * detDto.Factor,
                        FuelCost = detDto.FuelCost,
                        EnergyCost = detDto.EnergyCost,
                        OperatorLaborCost = detDto.OperatorLaborCost,
                        DepreciationCost = detDto.DepreciationCost,
                        RepairCost = detDto.RepairCost
                    };
                    _db.EstimateItemDetails.Add(detail);
                }
                await _db.SaveChangesAsync(ct);

                RecalculateItemTotals(item);
            }
        }

        RecalculateEstimateTotals(estimate.Id);
        await _db.SaveChangesAsync(ct);

        return estimate;
    }

    // ====================================================================
    // RECALCULATE
    // ====================================================================

    /// <summary>
    /// Tính lại MaterialTotal, LaborTotal, MachineTotal, TotalAmount và đơn giá
    /// cho 1 EstimateItem dựa trên Details của nó.
    /// </summary>
    public void RecalculateItemTotals(EstimateItem item)
    {
        var details = _db.EstimateItemDetails
            .Where(x => x.EstimateItemId == item.Id)
            .ToList();

        item.MaterialTotal = details.Where(x => x.DetailType == CAT_MATERIAL).Sum(x => x.TotalAmount);
        item.LaborTotal = details.Where(x => x.DetailType == CAT_LABOR).Sum(x => x.TotalAmount);
        item.MachineTotal = details.Where(x => x.DetailType == CAT_MACHINE).Sum(x => x.TotalAmount);
        item.TotalAmount = item.MaterialTotal + item.LaborTotal + item.MachineTotal;

        if (item.Quantity > 0)
        {
            item.MaterialUnitPrice = item.MaterialTotal / item.Quantity;
            item.LaborUnitPrice = item.LaborTotal / item.Quantity;
            item.MachineUnitPrice = item.MachineTotal / item.Quantity;
        }
    }

    /// <summary>
    /// Tính lại CostSummary (MaterialCost, LaborCost, …, RoundedAmount) và
    /// Estimate.TotalAmount + TotalAmountText. Lưu vào ChangeTracker, gọi SaveChangesAsync() sau.
    /// </summary>
    public void RecalculateEstimateTotals(int estimateId)
    {
        var items = _db.EstimateItems
            .Where(x => x.EstimateId == estimateId)
            .ToList();

        var summary = _db.CostSummaries.FirstOrDefault(x => x.EstimateId == estimateId)
                      ?? new CostSummary { EstimateId = estimateId };

        // I. Chi phí trực tiếp
        summary.MaterialCost = items.Sum(x => x.MaterialTotal);
        summary.LaborCost = items.Sum(x => x.LaborTotal);
        summary.MachineCost = items.Sum(x => x.MachineTotal);
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

        // Cập nhật Estimate header
        var estimate = _db.Estimates.Find(estimateId);
        if (estimate != null)
        {
            estimate.TotalAmount = summary.RoundedAmount;
            estimate.TotalAmountText = ConvertNumberToVietnameseText(summary.RoundedAmount);
        }

        if (summary.Id == 0)
            _db.CostSummaries.Add(summary);
    }

    // ====================================================================
    // NUMBER → VIETNAMESE TEXT (1 hàm duy nhất)
    // ====================================================================

    private static readonly string[] _digits =
        { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

    private static readonly string[] _units = { "", "nghìn", "triệu", "tỷ" };

    public string ConvertNumberToVietnameseText(decimal amount)
    {
        if (amount == 0) return "Không đồng";

        var number = (long)amount;
        var str = number.ToString();
        var result = string.Empty;

        if (str.Length <= 3)
        {
            result = ReadThreeDigits(str);
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
                    var val = ReadThreeDigits(g);
                    result += val + " " + _units[groups.Count - i - 1] + " ";
                }
            }
        }

        result = result.Trim();
        if (result.EndsWith(",")) result = result[..^1];

        if (result.Length > 0)
            result = char.ToUpper(result[0]) + result[1..];

        return result + " đồng";
    }

    private static string ReadThreeDigits(string s)
    {
        if (s.Length != 3) s = s.PadLeft(3, '0');

        var result = string.Empty;
        var a = int.Parse(s[0].ToString());
        var b = int.Parse(s[1].ToString());
        var c = int.Parse(s[2].ToString());

        if (a > 0) result += _digits[a] + " trăm";
        else if (b > 0 || c > 0) result += "không trăm";

        if (b > 0) result += " " + (b == 1 ? "mười" : _digits[b] + " mươi");
        else if (c > 0) result += " linh";

        if (b == 0 && c > 0)
            result += " " + _digits[c];
        else if (b == 1 && c > 0)
            result = result.TrimEnd() + " " + _digits[c];
        else if (b > 1 && c > 0)
            result += " " + _digits[c];

        return result.Trim();
    }

    // ====================================================================
    // MAP ENTITY → DTO
    // ====================================================================

    public EstimateDto MapToDto(Estimate e)
    {
        return new EstimateDto
        {
            Id = e.Id,
            EstimateCategoryId = e.EstimateCategoryId,
            DocumentType = e.DocumentType,
            DocumentNumber = e.DocumentNumber,
            DocumentDate = e.DocumentDate,
            TotalAmount = e.TotalAmount,
            TotalAmountText = e.TotalAmountText,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            Items = e.Items.Select(w => new EstimateItemDto
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
                Details = w.Details.Select(d => new EstimateItemDetailDto
                {
                    Id = d.Id,
                    Category = d.DetailType,
                    Code = d.Code,
                    Name = d.Name,
                    Unit = d.Unit,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Factor = d.Factor,
                    TotalAmount = d.TotalAmount,
                    FuelCost = d.FuelCost,
                    EnergyCost = d.EnergyCost,
                    OperatorLaborCost = d.OperatorLaborCost,
                    DepreciationCost = d.DepreciationCost,
                    RepairCost = d.RepairCost
                }).ToList()
            }).ToList(),
            CostSummary = e.CostSummary == null ? null : new CostSummaryDto
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
            }
        };
    }

    // ====================================================================
    // EXCEL IMPORT (chỉ chạy trên Windows — phụ thuộc Microsoft.Office.Interop)
    // ====================================================================

    /// <summary>
    /// Import dữ liệu từ file Excel. Tìm sheet theo TÊN (không phụ thuộc thứ tự),
    /// KHÔNG dùng index vì file có thể có nhiều sheet khác (Tổng hợp VT, Nhân công, Máy, …).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public async Task<int> ImportFromExcelAsync(string filePath, int estimateCategoryId, CancellationToken ct = default)
    {
        var excel = new Microsoft.Office.Interop.Excel.Application();
        excel.Visible = false;
        excel.DisplayAlerts = false;

        try
        {
            var wb = excel.Workbooks.Open(filePath);

            // Tìm sheet theo tên (chấp nhận nhiều biến thể có dấu / không dấu)
            var wsThkp = FindSheet(wb, "THKP hạng mục", "THKP Hang muc", "THKP");
            var wsGiaTH = FindSheet(wb, "Giá tổng hợp", "Gia tong hop", "Tổng hợp giá", "Tong hop gia");
            var wsDonGia = FindSheet(wb, "Đơn giá chi tiết", "Don gia chi tiet", "Đơn giá", "Don gia");

            if (wsThkp == null)
                throw new InvalidOperationException("Không tìm thấy sheet 'THKP hạng mục' trong file Excel");
            if (wsGiaTH == null)
                throw new InvalidOperationException("Không tìm thấy sheet 'Giá tổng hợp' trong file Excel");
            if (wsDonGia == null)
                throw new InvalidOperationException("Không tìm thấy sheet 'Đơn giá chi tiết' trong file Excel");

            int estimateIdInt = await ImportCostSummaryAsync(wsThkp, estimateCategoryId, ct);

            await ImportItemsAsync(wsGiaTH, estimateIdInt, ct);
            await ImportItemDetailsAsync(wsDonGia, estimateIdInt, ct);

            wb.Close(false);

            // Tính lại tất cả totals
            var items = await _db.EstimateItems
                .Where(x => x.EstimateId == estimateIdInt)
                .ToListAsync(ct);
            foreach (var item in items)
                RecalculateItemTotals(item);
            await _db.SaveChangesAsync(ct);

            RecalculateEstimateTotals(estimateIdInt);
            await _db.SaveChangesAsync(ct);

            return estimateIdInt;
        }
        finally
        {
            excel.Quit();
            Marshal.ReleaseComObject(excel);
        }
    }

    private async Task<int> ImportCostSummaryAsync(dynamic ws, int estimateCategoryId, CancellationToken ct)
    {
        string projectName = "", category = "";

        try { projectName = GetCellValue(ws, 3, 4); } catch { }
        try { category = GetCellValue(ws, 3, 1); } catch { }

        if (projectName.StartsWith("DỰ ÁN: "))
            projectName = projectName[7..];

        var estimate = new Estimate
        {
            EstimateCategoryId = estimateCategoryId,
            DocumentType = "M-02B"
        };

        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync(ct);

        var summary = new CostSummary { EstimateId = estimate.Id };

        try { summary.MaterialCost = ParseDecimal(GetCellValue(ws, 8, 8)); } catch { }
        try { summary.LaborCost = ParseDecimal(GetCellValue(ws, 10, 8)); } catch { }
        try { summary.MachineCost = ParseDecimal(GetCellValue(ws, 12, 8)); } catch { }
        try { summary.DirectCost = ParseDecimal(GetCellValue(ws, 14, 8)); } catch { }
        try { summary.GeneralCostRate = ParseDecimal(GetCellValue(ws, 16, 7)) / 100; } catch { }
        try { summary.GeneralCost = ParseDecimal(GetCellValue(ws, 16, 8)); } catch { }
        try { summary.OverheadCostRate = ParseDecimal(GetCellValue(ws, 17, 7)) / 100; } catch { }
        try { summary.OverheadCost = ParseDecimal(GetCellValue(ws, 17, 8)); } catch { }
        try { summary.UndeterminedCostRate = ParseDecimal(GetCellValue(ws, 18, 7)) / 100; } catch { }
        try { summary.UndeterminedCost = ParseDecimal(GetCellValue(ws, 18, 8)); } catch { }
        try { summary.IndirectCost = ParseDecimal(GetCellValue(ws, 19, 8)); } catch { }
        try { summary.PreTaxIncomeRate = ParseDecimal(GetCellValue(ws, 20, 7)) / 100; } catch { }
        try { summary.PreTaxIncome = ParseDecimal(GetCellValue(ws, 20, 8)); } catch { }
        try { summary.PreTaxAmount = ParseDecimal(GetCellValue(ws, 21, 8)); } catch { }
        try { summary.VatRate = ParseDecimal(GetCellValue(ws, 22, 7)) / 100; } catch { }
        try { summary.VatAmount = ParseDecimal(GetCellValue(ws, 22, 8)); } catch { }
        try { summary.PostTaxAmount = ParseDecimal(GetCellValue(ws, 23, 8)); } catch { }
        try { summary.RoundedAmount = ParseDecimal(GetCellValue(ws, 24, 8)); } catch { }

        _db.CostSummaries.Add(summary);
        await _db.SaveChangesAsync(ct);

        if (summary.RoundedAmount > 0)
        {
            estimate.TotalAmount = summary.RoundedAmount;
            estimate.TotalAmountText = ConvertNumberToVietnameseText(summary.RoundedAmount);
            await _db.SaveChangesAsync(ct);
        }

        return estimate.Id;
    }

    private async Task ImportItemsAsync(dynamic ws, int estimateId, CancellationToken ct)
    {
        var usedRange = ws.UsedRange;
        int rowCount = usedRange.Rows.Count;

        for (int r = 7; r <= rowCount; r++)
        {
            try
            {
                var sttValue = GetCellValue(ws, r, 1);
                if (string.IsNullOrWhiteSpace(sttValue)) continue;
                if (!int.TryParse(sttValue, out int sttNum)) continue;

                var item = new EstimateItem
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

                _db.EstimateItems.Add(item);
                await _db.SaveChangesAsync(ct);
            }
            catch { /* skip invalid row */ }
        }
    }

    private async Task ImportItemDetailsAsync(dynamic ws, int estimateId, CancellationToken ct)
    {
        var items = await _db.EstimateItems
            .Where(x => x.EstimateId == estimateId)
            .ToListAsync(ct);

        var usedRange = ws.UsedRange;
        int rowCount = usedRange.Rows.Count;

        int? currentItemId = null;
        for (int r = 6; r <= rowCount; r++)
        {
            try
            {
                var sttValue = GetCellValue(ws, r, 1);
                var codeValue = GetCellValue(ws, r, 2);

                int sttNum2 = 0;
                bool hasStt = !string.IsNullOrWhiteSpace(sttValue)
                              && int.TryParse(sttValue, out sttNum2);
                if (hasStt)
                {
                    var wi = items.FirstOrDefault(x => x.Stt == sttNum2);
                    if (wi != null) currentItemId = wi.Id;
                }

                if (currentItemId == null) continue;

                var category = GetCellValue(ws, r, 3);
                if (string.IsNullOrWhiteSpace(category)) continue;
                if (category.Contains("Cộng") || category.Contains("Tổng")) continue;

                string catType;
                if (category.StartsWith("a)") || category.Contains("Vật liệu") || category.Contains("VL"))
                    catType = CAT_MATERIAL;
                else if (category.StartsWith("b)") || category.Contains("Nhân công") || category.Contains("NC"))
                    catType = CAT_LABOR;
                else if (category.StartsWith("c)") || category.Contains("Máy") || category.Contains("M"))
                    catType = CAT_MACHINE;
                else
                    continue;

                var detail = new EstimateItemDetail
                {
                    EstimateItemId = currentItemId.Value,
                    DetailType = catType,
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
                    _db.EstimateItemDetails.Add(detail);
                    await _db.SaveChangesAsync(ct);
                }
            }
            catch { /* skip invalid row */ }
        }
    }

    private static dynamic? FindSheet(dynamic wb, params string[] candidates)
    {
        var sheets = wb.Sheets;
        int count = sheets.Count;

        for (int i = 1; i <= count; i++)
        {
            string? sheetName = sheets[i]?.Name?.ToString();
            if (string.IsNullOrWhiteSpace(sheetName)) continue;

            var normalized = NormalizeText(sheetName);
            foreach (var c in candidates)
            {
                if (normalized == NormalizeText(c) || normalized.Contains(NormalizeText(c)))
                    return sheets[i];
            }
        }
        return null;
    }

    private static string NormalizeText(string s)
    {
        s = s.Trim().ToLowerInvariant();
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
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
        value = value.Replace(",", "").Replace(" ", "").Replace(".", "").Trim();
        return decimal.TryParse(value, out var result) ? result : 0;
    }

    private static string ExtractCategory(string category)
    {
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
}
