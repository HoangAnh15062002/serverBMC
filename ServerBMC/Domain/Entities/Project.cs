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

    // Hợp đồng & Pháp lý
    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public decimal? TotalEstimateValue { get; set; }
    public decimal? GuaranteeValue { get; set; }
    public int? MaintenancePeriodMonths { get; set; }

    // Các bên liên quan
    public string? DesignUnit { get; set; }
    public string? SupervisionUnit { get; set; }
    public string? ProjectManager { get; set; }

    public User Creator { get; set; } = null!;
    public ICollection<ProjectLot> Lots { get; set; } = new List<ProjectLot>();
    public ICollection<PaymentPlan> PaymentPlans { get; set; } = new List<PaymentPlan>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
    public ICollection<Warning> Warnings { get; set; } = new List<Warning>();
}