namespace Entities_Dtos.DTOs.BalanceEnquiryDTOs;

public class QBEAccountCharge
{
    public Guid CustomerAccountId { get; set; }
    public string AccountNumber { get; set; }
    public decimal TotalCharges { get; set; }
    public int TransactionCount { get; set; }
}
