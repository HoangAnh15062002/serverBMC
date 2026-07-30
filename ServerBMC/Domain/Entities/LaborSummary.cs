using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Nhân công (bảng tham chiếu — global).
/// </summary>
[Table("LaborSummaries")]
public class LaborSummary
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Hệ số lương</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal SalaryFactor { get; set; }

    /// <summary>Đơn giá nhân công bình quân</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal AverageLaborPrice { get; set; }

    /// <summary>Hệ số lương bình quân</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal AverageSalaryFactor { get; set; }

    /// <summary>Đơn giá nhân công = SalaryFactor × AverageLaborPrice × AverageSalaryFactor</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(100)]
    public string? LaborGroup { get; set; } // BacThoCo / ThoKyThuat...

    [MaxLength(100)]
    public string? Region { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
}
