namespace ServerBMC.DTOs;

public class EstimateDto
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Investor { get; set; } = string.Empty;
    public string Consultant { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "M-02B";
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string TotalAmountText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EstimateWorkItemDto> WorkItems { get; set; } = new();
    public CostSummaryDto? CostSummary { get; set; }
}

public class EstimateWorkItemDto
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
    public List<WorkItemDetailDto> Details { get; set; } = new();
}

public class WorkItemDetailDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; }
    public decimal TotalAmount { get; set; }
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

public class CreateEstimateDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Investor { get; set; } = string.Empty;
    public string Consultant { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "M-02B";
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public List<CreateEstimateWorkItemDto> WorkItems { get; set; } = new();
}

public class CreateEstimateWorkItemDto
{
    public int Stt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public List<CreateWorkItemDetailDto> Details { get; set; } = new();
}

public class CreateWorkItemDetailDto
{
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; } = 1.0m;
}

public class UpdateEstimateDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Investor { get; set; } = string.Empty;
    public string Consultant { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
}

public class UpdateWorkItemDetailDto
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; } = 1.0m;
}

public class UpdateWorkItemDto
{
    public decimal Quantity { get; set; }
}

public class ImportExcelDto
{
    public string FilePath { get; set; } = string.Empty;
    public int? EstimateId { get; set; }
}
