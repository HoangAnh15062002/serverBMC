using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Hạng mục dự toán (bia, móng, thân, …).
/// Gắn với Project để biết dự toán thuộc dự án nào.
/// </summary>
[Table("EstimateCategories")]
public class EstimateCategory
{
    [Key]
    public int Id { get; set; }

    /// <summary>Thuộc dự án nào</summary>
    public int? ProjectId { get; set; }

    /// <summary>Tên hạng mục: bia, móng, thân, …</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả / ghi chú</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Thứ tự sắp xếp</summary>
    public int SortOrder { get; set; }

    /// <summary>Trạng thái: Hoạt động, Không hoạt động</summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Hoạt động";

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public User Creator { get; set; } = null!;
    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
}
