namespace ServerBMC.Models;

public class CostSummary
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    
    // I. Chi phí trực tiếp
    public decimal MaterialCost { get; set; }      // VL
    public decimal LaborCost { get; set; }        // NC  
    public decimal MachineCost { get; set; }       // M
    public decimal DirectCost { get; set; }        // T = VL + NC + M
    
    // II. Chi phí gián tiếp
    public decimal GeneralCost { get; set; }        // C (6.7%)
    public decimal GeneralCostRate { get; set; } = 0.067m;
    public decimal OverheadCost { get; set; }       // LT (1%)
    public decimal OverheadCostRate { get; set; } = 0.01m;
    public decimal UndeterminedCost { get; set; }   // TT (2.5%)
    public decimal UndeterminedCostRate { get; set; } = 0.025m;
    public decimal IndirectCost { get; set; }      // GT = C + LT + TT
    
    // III. Thu nhập chịu thuế
    public decimal PreTaxIncome { get; set; }      // TL (5.5%)
    public decimal PreTaxIncomeRate { get; set; } = 0.055m;
    public decimal PreTaxAmount { get; set; }      // G
    
    // IV. Thuế GTGT
    public decimal VatAmount { get; set; }         // GTGT (10%)
    public decimal VatRate { get; set; } = 0.10m;
    
    // V. Tổng cộng
    public decimal PostTaxAmount { get; set; }     // Gxd
    public decimal RoundedAmount { get; set; }
    
    public Estimate? Estimate { get; set; }
}
