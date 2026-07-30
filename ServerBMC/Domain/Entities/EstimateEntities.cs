using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

[Table("Estimates")]
public class Estimate
{
    [Key]
    public int Id { get; set; }

    public int EstimateCategoryId { get; set; }

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

    public EstimateCategory? EstimateCategory { get; set; }
    public User? Creator { get; set; }
    public ICollection<EstimateItem> Items { get; set; } = new List<EstimateItem>();
    public CostSummary? CostSummary { get; set; }
}

[Table("EstimateWorkItems")]
public class EstimateItem
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
    public ICollection<EstimateItemDetail> Details { get; set; } = new List<EstimateItemDetail>();
}

[Table("EstimateItemDetails")]
public class EstimateItemDetail
{
    [Key]
    public int Id { get; set; }

    public int EstimateItemId { get; set; }

    /// <summary>a) Vật liệu, b) Nhân công, c) Máy</summary>
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

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

    public EstimateItem? EstimateItem { get; set; }
    public ICollection<ItemMaterialDetail> MaterialDetails { get; set; } = new List<ItemMaterialDetail>();
    public ICollection<ItemLaborDetail> LaborDetails { get; set; } = new List<ItemLaborDetail>();
    public ICollection<ItemMachineDetail> MachineDetails { get; set; } = new List<ItemMachineDetail>();
}

[Table("CostSummaries")]
public class CostSummary
{
    [Key]
    public int Id { get; set; }

    public int EstimateId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }      // VL

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCost { get; set; }        // NC

    [Column(TypeName = "decimal(18,2)")]
    public decimal MachineCost { get; set; }       // M

    [Column(TypeName = "decimal(18,2)")]
    public decimal DirectCost { get; set; }        // T = VL + NC + M

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

    [Column(TypeName = "decimal(18,2)")]
    public decimal PreTaxIncome { get; set; }      // TL
    public decimal PreTaxIncomeRate { get; set; } = 0.055m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PreTaxAmount { get; set; }      // G

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }         // GTGT
    public decimal VatRate { get; set; } = 0.10m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PostTaxAmount { get; set; }     // Gxd

    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundedAmount { get; set; }

    public Estimate? Estimate { get; set; }
}
