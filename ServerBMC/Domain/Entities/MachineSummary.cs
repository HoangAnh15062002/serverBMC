using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Máy thi công (bảng tham chiếu — global).
/// 5 chi phí cố định: Nhiên liệu, Năng lượng, Nhân công vận hành, Khấu hao, Sửa chữa.
/// </summary>
[Table("MachineSummaries")]
public class MachineSummary
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Nhiên liệu (đ/lần)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal FuelCost { get; set; }

    /// <summary>Năng lượng (đ/lần)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal EnergyCost { get; set; }

    /// <summary>Nhân công vận hành máy (đ/ca)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal OperatorLaborCost { get; set; }

    /// <summary>Khấu hao (đ/lần)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal DepreciationCost { get; set; }

    /// <summary>Sửa chữa (đ/lần)</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal RepairCost { get; set; }

    /// <summary>Tổng chi phí máy = Fuel + Energy + Operator + Depreciation + Repair</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalUnitCost { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
}
