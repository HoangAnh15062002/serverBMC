namespace ServerBMC.Domain.Entities;

public class Warning
{
    public int Id { get; set; }
    public string WarningType { get; set; } = null!; // CostOverrun / ScheduleDelay / etc.
    public int WarningLevel { get; set; } // 1: Info, 2: Yellow, 3: Orange, 4: Red
    public int? ProjectId { get; set; }
    public int? CategoryId { get; set; }
    public int? WorkItemId { get; set; }
    public int? LotId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public User? Resolver { get; set; }
    public Category? Category { get; set; }
    public ProjectWorkItem? WorkItem { get; set; }
    public ProjectLot? Lot { get; set; }
}