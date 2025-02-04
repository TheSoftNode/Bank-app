namespace Entities_Dtos.Responses;

public class UnchargedEnquiryResponse
{
    public Guid EnquiryId { get; set; }
    public string? CustomerNumber { get; set; }
    public string? AccountNumber { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal SessionCharge { get; set; }
    public string? TelcoProvider { get; set; }
    public DateTime CreatedAt { get; set; }
}
