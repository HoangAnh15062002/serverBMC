using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Giá tháng của vật liệu (bảng tham chiếu — global, có version theo tháng).
/// </summary>
[Table("MonthlyPrices")]
public class MonthlyPrice
{
    [Key]
    public int Id { get; set; }

    /// <summary>Tháng áp dụng (định dạng: yyyy-MM)</summary>
    [MaxLength(7)]
    public string EffectiveMonth { get; set; } = string.Empty;

    /// <summary>Mã vật liệu</summary>
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên vật liệu</summary>
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Giá tháng</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal MonthlyPriceValue { get; set; }

    /// <summary>Hệ số</summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal Factor { get; set; } = 1.0m;

    /// <summary>Giá chính</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal MainPrice { get; set; }

    /// <summary>Giá sau VAT</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal PriceAfterVat { get; set; }

    /// <summary>Mã chuẩn</summary>
    [MaxLength(50)]
    public string? StandardCode { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; } // Vùng áp dụng (Hà Nội, TP.HCM...)

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
}
