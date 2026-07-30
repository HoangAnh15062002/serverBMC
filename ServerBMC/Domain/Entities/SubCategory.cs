namespace ServerBMC.Domain.Entities;

public class SubCategory
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string SubCategoryCode { get; set; } = null!;
    public string SubCategoryName { get; set; } = null!;
    public string? SubCategoryType { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Chưa bắt đầu";
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<ProjectWorkItem> ProjectWorkItems { get; set; } = new List<ProjectWorkItem>();
}