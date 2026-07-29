namespace ServerBMC.Models;

public class WorkItem
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    public int Stt { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MaterialUnitPrice { get; set; }
    public decimal LaborUnitPrice { get; set; }
    public decimal MachineUnitPrice { get; set; }
    public decimal MaterialTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal MachineTotal { get; set; }
    public decimal TotalAmount { get; set; }
    
    public Estimate? Estimate { get; set; }
    public ICollection<WorkItemDetail> Details { get; set; } = new List<WorkItemDetail>();
}
