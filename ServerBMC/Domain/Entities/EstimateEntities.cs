using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

[Table("Estimates")]
public class Estimate
{
    [Key]
    public int Id { get; set; }
    
    [MaxLength(200)]
    public string ProjectName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Location { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Investor { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Consultant { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Scope { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string DocumentType { get; set; } = "M-02B";
    
    [MaxLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string DocumentDate { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [MaxLength(200)]
    public string TotalAmountText { get; set; } = string.Empty;
    
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<EstimateWorkItem> WorkItems { get; set; } = new List<EstimateWorkItem>();
    public CostSummary? CostSummary { get; set; }
    
    public User? Creator { get; set; }
}

[Table("EstimateWorkItems")]
public class EstimateWorkItem
{
    [Key]
    public int Id { get; set; }
    
    public int EstimateId { get; set; }
    
    public int Stt { get; set; }
    
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal MaterialUnitPrice { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal LaborUnitPrice { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal MachineUnitPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialTotal { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborTotal { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MachineTotal { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    public Estimate? Estimate { get; set; }
    public ICollection<WorkItemDetail> Details { get; set; } = new List<WorkItemDetail>();
}

[Table("WorkItemDetails")]
public class WorkItemDetail
{
    [Key]
    public int Id { get; set; }
    
    public int WorkItemId { get; set; }
    
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty; // Vật liệu, Nhân công, Máy
    
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }
    
    [Column(TypeName = "decimal(8,4)")]
    public decimal Factor { get; set; } = 1.0m;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    public EstimateWorkItem? WorkItem { get; set; }
}

[Table("CostSummaries")]
public class CostSummary
{
    [Key]
    public int Id { get; set; }
    
    public int EstimateId { get; set; }
    
    // I. Chi phí trực tiếp
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }      // VL
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCost { get; set; }        // NC
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MachineCost { get; set; }       // M
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DirectCost { get; set; }        // T = VL + NC + M
    
    // II. Chi phí gián tiếp
    [Column(TypeName = "decimal(18,2)")]
    public decimal GeneralCost { get; set; }        // C
    public decimal GeneralCostRate { get; set; } = 0.067m;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal OverheadCost { get; set; }       // LT
    public decimal OverheadCostRate { get; set; } = 0.01m;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UndeterminedCost { get; set; }   // TT
    public decimal UndeterminedCostRate { get; set; } = 0.025m;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal IndirectCost { get; set; }      // GT
    
    // III. Thu nhập chịu thuế
    [Column(TypeName = "decimal(18,2)")]
    public decimal PreTaxIncome { get; set; }      // TL
    public decimal PreTaxIncomeRate { get; set; } = 0.055m;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PreTaxAmount { get; set; }      // G
    
    // IV. Thuế GTGT
    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }         // GTGT
    public decimal VatRate { get; set; } = 0.10m;
    
    // V. Tổng cộng
    [Column(TypeName = "decimal(18,2)")]
    public decimal PostTaxAmount { get; set; }     // Gxd
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundedAmount { get; set; }
    
    public Estimate? Estimate { get; set; }
}
