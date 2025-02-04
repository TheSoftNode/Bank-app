

namespace Entities_Dtos.DTOs;

public class DailyReconciliationDto
{
    public DateTime Date { get; set; }
    public decimal TotalSMSCharges { get; set; }
    public decimal TotalQBECharges { get; set; }
    public decimal TotalVatCollected { get; set; }
    public decimal TotalTelcoCharges { get; set; }
    public Dictionary<string, decimal> TelcoProviderCharges { get; set; }
}
