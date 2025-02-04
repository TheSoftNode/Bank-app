namespace Entities_Dtos.Responses;

public class ProcessedEnquiryResponse
{
    public Guid EnquiryId { get; set; }
    public string? CustomerNumber { get; set; }
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal SessionCharge { get; set; }
    public bool IsCharged { get; set; }
    public DateTime ProcessedDate { get; set; }
}
