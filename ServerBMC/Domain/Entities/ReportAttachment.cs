namespace ServerBMC.Domain.Entities;

public class ReportAttachment
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileOriginalName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public string FileType { get; set; } = null!;
    public string? FileCategory { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Report Report { get; set; } = null!;
    public User Uploader { get; set; } = null!;
}