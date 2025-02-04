namespace Entities_Dtos.Responses;

public class QuickBalanceEnquiryResponse
{
    public Guid EnquiryId { get; set; }
    public string CustomerNumber { get; set; }
    public string AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal SessionCharge { get; set; }
    public string TelcoProvider { get; init; }
    public bool IsCharged { get; set; }
    public DateTime CreatedAt { get; init; }
}
