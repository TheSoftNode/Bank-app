namespace Entities_Dtos.DTOs.BalanceEnquiryDTOs;

public class QBECustomerSummary
{
    public Guid CustomerId { get; set; }
    public string CustomerNumber { get; set; }
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public decimal TotalCharges { get; set; }
    public int TransactionCount { get; set; }
    public DateTime LastTransactionDate { get; set; }
}
