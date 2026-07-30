using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Hao phí vật tư (bảng tham chiếu — global).
/// Định mức hao phí vật liệu, nhân công, máy cho từng công tác.
/// </summary>
[Table("MaterialNorms")]
public class MaterialNorm
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên công tác</summary>
    [MaxLength(300)]
    public string WorkName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    /// <summary>Định mức VL</summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal MaterialNormValue { get; set; }

    /// <summary>Định mức NC</summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal LaborNormValue { get; set; }

    /// <summary>Định mức Máy</summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal MachineNormValue { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal Factor { get; set; } = 1.0m;

    /// <summary>KL hao phí VL</summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal MaterialLossQuantity { get; set; }

    /// <summary>KL hao phí NC</summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal LaborLossQuantity { get; set; }

    /// <summary>KL hao phí Máy</summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal MachineLossQuantity { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
}
