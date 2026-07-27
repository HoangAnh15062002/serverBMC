using System.ComponentModel.DataAnnotations;

namespace ServerBMC.DTOs;

public class CategoryCreateDto
{
    [Required, MaxLength(50)] public string CategoryCode { get; set; } = null!;
    [Required, MaxLength(255)] public string CategoryName { get; set; } = null!;
    public string? CategoryType { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public string? Description { get; set; }
}

public class CategoryUpdateDto
{
    [Required, MaxLength(255)] public string CategoryName { get; set; } = null!;
    public string? CategoryType { get; set; }
    public int SortOrder { get; set; }
    public string? Status { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal? ProgressPercent { get; set; }
    public string? Description { get; set; }
}

public class SubCategoryCreateDto
{
    [Required, MaxLength(50)] public string SubCategoryCode { get; set; } = null!;
    [Required, MaxLength(255)] public string SubCategoryName { get; set; } = null!;
    public string? SubCategoryType { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public string? Description { get; set; }
}

public class SubCategoryUpdateDto
{
    [Required, MaxLength(255)] public string SubCategoryName { get; set; } = null!;
    public string? SubCategoryType { get; set; }
    public int SortOrder { get; set; }
    public string? Status { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public decimal? ProgressPercent { get; set; }
    public string? Description { get; set; }
}