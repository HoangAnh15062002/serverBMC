namespace ServerBMC.Domain.Entities;

public class AcceptedQuantity
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public DateTime AcceptanceDate { get; set; }
    public decimal AcceptedQuantityValue { get; set; }
    public string? AcceptanceMinutes { get; set; }
    public string? Inspector { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProjectWorkItem WorkItem { get; set; } = null!;
    public User Creator { get; set; } = null!;
}