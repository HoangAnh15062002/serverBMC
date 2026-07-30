namespace ServerBMC.Domain.Entities;

public class ProjectLot
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string LotCode { get; set; } = null!;
    public string LotName { get; set; } = null!;
    public string? LotType { get; set; }
    public decimal? Area { get; set; }
    public int? FloorCount { get; set; }
    public int? UnitCount { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Chưa triển khai";
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
}