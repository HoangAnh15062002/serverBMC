using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Tổng hợp vật tư (bảng tham chiếu — global, dùng chung mọi Estimate).
/// </summary>
[Table("MaterialSummaries")]
public class MaterialSummary
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AveragePrice { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal Factor { get; set; } = 1.0m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal CarFare { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DeliveredPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; } // VatLieu / ThietBi / NhienLieu

    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
}
