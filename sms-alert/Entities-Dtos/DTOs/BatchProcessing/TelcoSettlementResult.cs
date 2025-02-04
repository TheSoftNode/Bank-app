namespace Entities_Dtos.DTOs.BatchProcessing;

public class TelcoSettlementResult
{
    public DateTime SettlementDate { get; set; }
    public int ProvidersProcessed { get; set; }
    public decimal TotalSettlementAmount { get; set; }
}
