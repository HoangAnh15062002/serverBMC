namespace ServerBMC.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string ProjectCode { get; set; } = null!;
    public string ProjectName { get; set; } = null!;
    public string? ProjectType { get; set; }
    public string? Location { get; set; }
    public string? Investor { get; set; }
    public string? Contractor { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Đang thi công";
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Creator { get; set; } = null!;
    public ICollection<ProjectLot> Lots { get; set; } = new List<ProjectLot>();
    public ICollection<PaymentPlan> PaymentPlans { get; set; } = new List<PaymentPlan>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}