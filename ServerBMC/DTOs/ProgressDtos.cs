using System.ComponentModel.DataAnnotations;

namespace ServerBMC.DTOs;

public class ProgressCreateDto
{
    [Required] public DateTime ProgressDate { get; set; }
    [Required] public decimal ProgressPercent { get; set; }
    public decimal? PlannedPercent { get; set; }
    public string? Notes { get; set; }
}

public record ProgressWarningDto(
    int CategoryId,
    string CategoryName,
    string LotName,
    string ProjectName,
    DateTime? PlannedEndDate,
    decimal ActualPercent,
    decimal PlannedPercent,
    int DaysOverdue,
    string Level); // Early / Late / Overdue / None