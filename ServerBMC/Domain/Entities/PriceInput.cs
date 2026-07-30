using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Đầu vào giá (bảng tham chiếu — global, có version theo tháng).
/// Các hệ số dùng để tính giá nhân công, máy, vật liệu.
/// </summary>
[Table("PriceInputs")]
public class PriceInput
{
    [Key]
    public int Id { get; set; }

    /// <summary>Tháng áp dụng (định dạng: yyyy-MM)</summary>
    [MaxLength(7)]
    public string EffectiveMonth { get; set; } = string.Empty;

    /// <summary>Tên thông số: Đơn giá nhiên liệu, Mức lương tối thiểu, Tỷ lệ phụ cấp…</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Giá trị</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    /// <summary>Đơn vị</summary>
    [MaxLength(20)]
    public string? Unit { get; set; }

    /// <summary>Loại: Fuel, Salary, Allowance, NormFactor, RecoveryRate…</summary>
    [MaxLength(50)]
    public string? InputType { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
}
