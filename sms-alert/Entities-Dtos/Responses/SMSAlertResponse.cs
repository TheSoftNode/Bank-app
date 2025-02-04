namespace Entities_Dtos.Responses;

public class SMSAlertResponse
{
    public Guid AlertId { get; set; }
    public string Email { get; set; }
    public string AccountNumber { get; set; }
    public string DeliveryStatus { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal VatAmount { get; set; }
    public DateTime DeliveryTimestamp { get; set; }
    public bool IsCharged { get; set; }
}
