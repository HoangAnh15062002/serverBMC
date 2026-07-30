namespace ServerBMC.DTOs;

// ====================================================================
// ESTIMATE CATEGORY (Hạng mục dự toán)
// ====================================================================

public class EstimateCategoryDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Hoạt động";
    public DateTime CreatedAt { get; set; }
}

public class CreateEstimateCategoryDto
{
    public int? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Hoạt động";
}

public class UpdateEstimateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Hoạt động";
}

// ====================================================================
// ESTIMATE
// ====================================================================

public class EstimateDto
{
    public int Id { get; set; }
    public int EstimateCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string DocumentType { get; set; } = "M-02B";
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string TotalAmountText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EstimateItemDto> Items { get; set; } = new();
    public CostSummaryDto? CostSummary { get; set; }
}

public class EstimateItemDto
{
    public int Id { get; set; }
    public int Stt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MaterialUnitPrice { get; set; }
    public decimal LaborUnitPrice { get; set; }
    public decimal MachineUnitPrice { get; set; }
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal MachineTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public List<EstimateItemDetailDto> Details { get; set; } = new();
}

public class EstimateItemDetailDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty; // a) Vật liệu, b) Nhân công, c) Máy
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ItemMaterialDetailDto> MaterialDetails { get; set; } = new();
    public List<ItemLaborDetailDto> LaborDetails { get; set; } = new();
    public List<ItemMachineDetailDto> MachineDetails { get; set; } = new();
}

public class ItemMaterialDetailDto
{
    public int Id { get; set; }
    public int? MaterialSummaryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ItemLaborDetailDto
{
    public int Id { get; set; }
    public int? LaborSummaryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ItemMachineDetailDto
{
    public int Id { get; set; }
    public int? MachineSummaryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal FuelCost { get; set; }
    public decimal EnergyCost { get; set; }
    public decimal OperatorLaborCost { get; set; }
    public decimal DepreciationCost { get; set; }
    public decimal RepairCost { get; set; }
}

public class CostSummaryDto
{
    public int Id { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal DirectCost { get; set; }
    public decimal GeneralCost { get; set; }
    public decimal GeneralCostRate { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal OverheadCostRate { get; set; }
    public decimal UndeterminedCost { get; set; }
    public decimal UndeterminedCostRate { get; set; }
    public decimal IndirectCost { get; set; }
    public decimal PreTaxIncome { get; set; }
    public decimal PreTaxIncomeRate { get; set; }
    public decimal PreTaxAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal PostTaxAmount { get; set; }
    public decimal RoundedAmount { get; set; }
}

// ====================================================================
// CREATE / UPDATE DTOs
// ====================================================================

public class CreateEstimateDto
{
    public int EstimateCategoryId { get; set; }
    public string DocumentType { get; set; } = "M-02B";
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public List<CreateEstimateItemDto> Items { get; set; } = new();
}

public class CreateEstimateItemDto
{
    public int Stt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public List<CreateEstimateItemDetailDto> Details { get; set; } = new();
}

public class CreateEstimateItemDetailDto
{
    public string Category { get; set; } = string.Empty; // a) Vật liệu, b) Nhân công, c) Máy
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; } = 1.0m;
}

public class UpdateEstimateDto
{
    public string DocumentType { get; set; } = "M-02B";
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
}

public class UpdateEstimateItemDetailDto
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; } = 1.0m;
}

public class UpdateEstimateItemDto
{
    public decimal Quantity { get; set; }
}

public class ImportExcelDto
{
    public string FilePath { get; set; } = string.Empty;
    public int? EstimateCategoryId { get; set; }
}

// ====================================================================
// REFERENCE TABLES (Global)
// ====================================================================

public class MaterialSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Factor { get; set; }
    public decimal CarFare { get; set; }
    public decimal DeliveredPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class CreateMaterialSummaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Factor { get; set; } = 1.0m;
    public decimal CarFare { get; set; }
}

public class LaborSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalaryFactor { get; set; }
    public decimal AverageLaborPrice { get; set; }
    public decimal AverageSalaryFactor { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class CreateLaborSummaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalaryFactor { get; set; }
    public decimal AverageLaborPrice { get; set; }
    public decimal AverageSalaryFactor { get; set; }
}

public class MachineSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal FuelCost { get; set; }
    public decimal EnergyCost { get; set; }
    public decimal OperatorLaborCost { get; set; }
    public decimal DepreciationCost { get; set; }
    public decimal RepairCost { get; set; }
    public decimal TotalUnitCost { get; set; }
    public string? Notes { get; set; }
}

public class CreateMachineSummaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal FuelCost { get; set; }
    public decimal EnergyCost { get; set; }
    public decimal OperatorLaborCost { get; set; }
    public decimal DepreciationCost { get; set; }
    public decimal RepairCost { get; set; }
}

public class MonthlyPriceDto
{
    public int Id { get; set; }
    public string EffectiveMonth { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal MonthlyPriceValue { get; set; }
    public decimal Factor { get; set; }
    public decimal MainPrice { get; set; }
    public decimal PriceAfterVat { get; set; }
    public string? StandardCode { get; set; }
    public string? Notes { get; set; }
}

public class CreateMonthlyPriceDto
{
    public string EffectiveMonth { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal MonthlyPriceValue { get; set; }
    public decimal Factor { get; set; } = 1.0m;
    public decimal MainPrice { get; set; }
    public decimal PriceAfterVat { get; set; }
    public string? StandardCode { get; set; }
}

public class PriceInputDto
{
    public int Id { get; set; }
    public string EffectiveMonth { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public string? InputType { get; set; }
    public string? Notes { get; set; }
}

public class CreatePriceInputDto
{
    public string EffectiveMonth { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public string? InputType { get; set; }
}

public class MaterialNormDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string WorkName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MaterialNormValue { get; set; }
    public decimal LaborNormValue { get; set; }
    public decimal MachineNormValue { get; set; }
    public decimal Factor { get; set; }
    public decimal MaterialLossQuantity { get; set; }
    public decimal LaborLossQuantity { get; set; }
    public decimal MachineLossQuantity { get; set; }
    public string? Notes { get; set; }
}

public class CreateMaterialNormDto
{
    public string Code { get; set; } = string.Empty;
    public string WorkName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MaterialNormValue { get; set; }
    public decimal LaborNormValue { get; set; }
    public decimal MachineNormValue { get; set; }
    public decimal Factor { get; set; } = 1.0m;
    public decimal MaterialLossQuantity { get; set; }
    public decimal LaborLossQuantity { get; set; }
    public decimal MachineLossQuantity { get; set; }
}
