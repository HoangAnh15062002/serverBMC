using System.ComponentModel.DataAnnotations;

namespace ServerBMC.DTOs;

public class PaymentPlanCreateDto
{
    [Required] public int ProjectId { get; set; }
    [Required, MaxLength(255)] public string PlanTitle { get; set; } = null!;
    [Required] public decimal PlanAmount { get; set; }
    [Required] public DateTime PlannedDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? ContractNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
}

public class PaymentPlanUpdateDto
{
    [Required, MaxLength(255)] public string PlanTitle { get; set; } = null!;
    [Required] public decimal PlanAmount { get; set; }
    [Required] public DateTime PlannedDate { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? ContractNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
}