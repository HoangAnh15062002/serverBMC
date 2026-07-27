using System.ComponentModel.DataAnnotations;

namespace ServerBMC.DTOs;

public class ReportCreateDto
{
    [Required, MaxLength(50)] public string ReportCode { get; set; } = null!;
    [Required, MaxLength(500)] public string ReportTitle { get; set; } = null!;
    [Required, MaxLength(50)] public string ReportType { get; set; } = null!;
    public int? ProjectId { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public string? Content { get; set; }
}

public class ReportUpdateDto
{
    [Required, MaxLength(500)] public string ReportTitle { get; set; } = null!;
    public string? Content { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
}

public class ApprovalRequestDto
{
    [Required, MaxLength(50)] public string ApprovalStatus { get; set; } = null!; // Duyet / TuChoi
    public string? Comments { get; set; }
}

public record ReportAttachmentDto(
    int Id, string FileName, string FileOriginalName, long FileSize, string FileType,
    string? FileCategory, DateTime UploadedAt, int UploadedBy);