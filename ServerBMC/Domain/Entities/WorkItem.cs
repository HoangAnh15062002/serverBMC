namespace ServerBMC.Domain.Entities;

public class WorkItem
{
    public int Id { get; set; }
    public int SubCategoryId { get; set; }
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal? StandardQuantity { get; set; }
    public decimal? MaterialNorm { get; set; }
    public decimal? LaborNorm { get; set; }
    public decimal? MachineNorm { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SubCategory SubCategory { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<UnitPrice> UnitPrices { get; set; } = new List<UnitPrice>();
    public ICollection<ActualCost> ActualCosts { get; set; } = new List<ActualCost>();
    public ICollection<AcceptedQuantity> AcceptedQuantities { get; set; } = new List<AcceptedQuantity>();
}