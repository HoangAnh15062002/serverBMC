using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerBMC.Domain.Entities;

/// <summary>
/// Chi tiết vật liệu của 1 ItemDetail.
/// Có thể reference đến MaterialSummary HOẶC nhập inline.
/// </summary>
[Table("ItemMaterialDetails")]
public class ItemMaterialDetail
{
    [Key]
    public int Id { get; set; }

    public int ItemDetailId { get; set; }

    /// <summary>FK tham chiếu — có thể null nếu nhập inline</summary>
    public int? MaterialSummaryId { get; set; }

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

    [Column(TypeName = "decimal(5,4)")]
    public decimal Factor { get; set; } = 1.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public EstimateItemDetail? ItemDetail { get; set; }
    public MaterialSummary? MaterialSummary { get; set; }
}
