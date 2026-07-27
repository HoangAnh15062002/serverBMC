namespace ServerBMC.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public int ProjectLotId { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string? CategoryType { get; set; }
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

    public ProjectLot ProjectLot { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    public ICollection<Progress> Progresses { get; set; } = new List<Progress>();
}