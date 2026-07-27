namespace ServerBMC.Domain.Entities;

public class Report
{
    public int Id { get; set; }
    public string ReportCode { get; set; } = null!;
    public string ReportTitle { get; set; } = null!;
    public string ReportType { get; set; } = null!; // TaiChinh / TienDo / GiaiNgan / CostCompare
    public int? ProjectId { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public string? Content { get; set; }
    public string Status { get; set; } = "Nháp"; // Nhap / ChoDuyet / DaDuyet / TuChoi
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public User Creator { get; set; } = null!;
    public ICollection<ReportAttachment> Attachments { get; set; } = new List<ReportAttachment>();
    public ICollection<ReportApproval> Approvals { get; set; } = new List<ReportApproval>();
}