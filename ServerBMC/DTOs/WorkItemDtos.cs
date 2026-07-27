using System.ComponentModel.DataAnnotations;

namespace ServerBMC.DTOs;

public class WorkItemCreateDto
{
    [Required, MaxLength(50)] public string ItemCode { get; set; } = null!;
    [Required, MaxLength(500)] public string ItemName { get; set; } = null!;
    [Required, MaxLength(20)] public string Unit { get; set; } = null!;
    public decimal? StandardQuantity { get; set; }
    public decimal? MaterialNorm { get; set; }
    public decimal? LaborNorm { get; set; }
    public decimal? MachineNorm { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
}

public class WorkItemUpdateDto
{
    [Required, MaxLength(500)] public string ItemName { get; set; } = null!;
    [Required, MaxLength(20)] public string Unit { get; set; } = null!;
    public decimal? StandardQuantity { get; set; }
    public decimal? MaterialNorm { get; set; }
    public decimal? LaborNorm { get; set; }
    public decimal? MachineNorm { get; set; }
    public int SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class UnitPriceCreateDto
{
    [Required, MaxLength(20)] public string PriceType { get; set; } = null!; // VL/NC/May/Khac
    [Required] public decimal UnitPriceValue { get; set; }
    [Required] public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

public class ActualCostCreateDto
{
    [Required, MaxLength(20)] public string CostType { get; set; } = null!;
    [Required] public DateTime CostDate { get; set; }
    [Required] public decimal Quantity { get; set; }
    [Required] public decimal UnitPriceValue { get; set; }
    [Required] public decimal TotalAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Supplier { get; set; }
    public string? Description { get; set; }
}

public class AcceptedQuantityCreateDto
{
    [Required] public DateTime AcceptanceDate { get; set; }
    [Required] public decimal AcceptedQuantityValue { get; set; }
    public string? AcceptanceMinutes { get; set; }
    public string? Inspector { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Lãi/lỗ của 1 WorkItem cụ thể.
/// </summary>
public record WorkItemProfitDto(
    int WorkItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    decimal AcceptedQuantity,
    decimal BidUnitPrice,
    decimal BidRevenue,
    decimal ActualCostTotal,
    decimal Profit,
    decimal CostPercent,
    string WarningLevel); // None / Yellow / Red

/// <summary>
/// Bảng tổng hợp lãi/lỗ theo Hạng mục × Loại chi phí (Module 6.1).
/// </summary>
public record CategoryCostCompareDto(
    int CategoryId,
    string CategoryName,
    decimal BidRevenue,
    decimal BidMaterialCost,
    decimal BidLaborCost,
    decimal BidMachineCost,
    decimal ActualMaterialCost,
    decimal ActualLaborCost,
    decimal ActualMachineCost,
    decimal ActualOtherCost,
    decimal Profit);

/// <summary>
/// Tổng hợp lãi/lỗ theo cấp.
/// </summary>
public record ProfitSummaryDto(
    int? ProjectId,
    int? LotId,
    int? CategoryId,
    decimal BidRevenue,
    decimal ActualCostTotal,
    decimal Profit);