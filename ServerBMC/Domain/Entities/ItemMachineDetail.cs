using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Chi tiết máy của 1 ItemDetail.
/// 5 chi phí cố định: Nhiên liệu, Năng lượng, Nhân công VH, Khấu hao, Sửa chữa.
/// Có thể reference đến MachineSummary HOẶC nhập inline.
/// </summary>
[Table("ItemMachineDetails")]
public class ItemMachineDetail
{
    [Key]
    public int Id { get; set; }

    public int ItemDetailId { get; set; }

    public int? MachineSummaryId { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    /// <summary>Tổng đơn giá = Fuel + Energy + Operator + Depreciation + Repair</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal Factor { get; set; } = 1.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // Chi tiết 5 chi phí máy (null nếu reference)
    [Column(TypeName = "decimal(18,4)")]
    public decimal FuelCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal EnergyCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OperatorLaborCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DepreciationCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RepairCost { get; set; }

    public EstimateItemDetail? ItemDetail { get; set; }
    public MachineSummary? MachineSummary { get; set; }
}
