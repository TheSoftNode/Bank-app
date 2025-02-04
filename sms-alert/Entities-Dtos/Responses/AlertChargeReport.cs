

namespace Entities_Dtos.Responses;

public class AlertChargeReport
{
    public DateTime Date { get; set; }
    public int TotalAlerts { get; set; }
    public int SuccessfulCharges { get; set; }
    public int FailedCharges { get; set; }
    public decimal TotalChargeAmount { get; set; }
    public decimal TotalVatAmount { get; set; }
    public List<FailedChargeDetail> FailedChargeDetails { get; set; }
}
