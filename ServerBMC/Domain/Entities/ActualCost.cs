namespace ServerBMC.Domain.Entities;

public class ActualCost
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public int? MaterialSummaryId { get; set; }
    public int? LotId { get; set; }
    public int? CategoryId { get; set; }
    public string CostType { get; set; } = null!; // VL / NC / May / Khac
    public DateTime CostDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPriceValue { get; set; }
    public decimal TotalAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Supplier { get; set; }
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProjectWorkItem WorkItem { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public MaterialSummary? MaterialSummary { get; set; }
    public ProjectLot? Lot { get; set; }
    public Category? Category { get; set; }
}