using System.ComponentModel.DataAnnotations;

namespace ServerBMC.DTOs;

public class ProjectCreateDto
{
    [Required, MaxLength(50)] public string ProjectCode { get; set; } = null!;
    [Required, MaxLength(500)] public string ProjectName { get; set; } = null!;
    public string? ProjectType { get; set; }
    public string? Location { get; set; }
    public string? Investor { get; set; }
    public string? Contractor { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}

public class ProjectUpdateDto
{
    [Required, MaxLength(500)] public string ProjectName { get; set; } = null!;
    public string? ProjectType { get; set; }
    public string? Location { get; set; }
    public string? Investor { get; set; }
    public string? Contractor { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}

public class ProjectLotCreateDto
{
    [Required, MaxLength(50)] public string LotCode { get; set; } = null!;
    [Required, MaxLength(255)] public string LotName { get; set; } = null!;
    public string? LotType { get; set; }
    public decimal? Area { get; set; }
    public int? FloorCount { get; set; }
    public int? UnitCount { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}

public class ProjectLotUpdateDto
{
    [Required, MaxLength(255)] public string LotName { get; set; } = null!;
    public string? LotType { get; set; }
    public decimal? Area { get; set; }
    public int? FloorCount { get; set; }
    public int? UnitCount { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}