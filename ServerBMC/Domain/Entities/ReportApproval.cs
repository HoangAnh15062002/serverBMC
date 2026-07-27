namespace ServerBMC.Domain.Entities;

public class ReportApproval
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int ApproverId { get; set; }
    public string ApprovalStatus { get; set; } = null!; // ChoDuyet / Duyet / TuChoi
    public string? Comments { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    public Report Report { get; set; } = null!;
    public User Approver { get; set; } = null!;
}