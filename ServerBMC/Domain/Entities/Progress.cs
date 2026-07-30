namespace ServerBMC.Domain.Entities;

public class Progress
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public DateTime ProgressDate { get; set; }
    public decimal ProgressPercent { get; set; }
    public decimal? PlannedPercent { get; set; }
    public decimal? Variance { get; set; }
    public string? Notes { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Images { get; set; } // JSON array of image URLs
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public User? Verifier { get; set; }
}