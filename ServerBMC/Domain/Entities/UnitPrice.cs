namespace ServerBMC.Domain.Entities;

public class UnitPrice
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public string PriceType { get; set; } = null!; // VL / NC / May / Khac
    public decimal UnitPriceValue { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProjectWorkItem WorkItem { get; set; } = null!;
    public User Creator { get; set; } = null!;
}