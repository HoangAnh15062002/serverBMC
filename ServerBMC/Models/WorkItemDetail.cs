namespace ServerBMC.Models;

public class WorkItemDetail
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public string Category { get; set; } = string.Empty; // Vật liệu, Nhân công, Máy
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Factor { get; set; } = 1.0m;
    public decimal TotalAmount { get; set; }
    
    public WorkItem? WorkItem { get; set; }
}
