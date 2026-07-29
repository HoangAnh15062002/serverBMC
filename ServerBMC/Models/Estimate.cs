namespace ServerBMC.Models;

public class Estimate
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Investor { get; set; } = string.Empty;
    public string Consultant { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string TotalAmountText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public CostSummary? CostSummary { get; set; }
}
