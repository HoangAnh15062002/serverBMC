namespace ServerBMC.Domain.Entities;

public class PaymentPlan
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string PlanTitle { get; set; } = null!;
    public decimal PlanAmount { get; set; }
    public DateTime PlannedDate { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime? ActualDate { get; set; }
    public string PaymentStatus { get; set; } = "Chưa giải ngân";
    public string PaymentType { get; set; } = "ThanhToanDot"; // TamUng / ThanhToanDot / QuyetToan / BaoLanh / BaoHanh
    public string? PaymentMethod { get; set; } // ChuyenKhoan / TienMat / ThanhToanBu
    public string? BankAccount { get; set; }
    public int? PaymentStage { get; set; }
    public string? ContractNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User Creator { get; set; } = null!;
}