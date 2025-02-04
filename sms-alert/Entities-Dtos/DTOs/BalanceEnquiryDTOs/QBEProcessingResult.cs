namespace Entities_Dtos.DTOs.BalanceEnquiryDTOs;

public class QBEProcessingResult
{
    public DateTime ProcessingDate { get; set; }
    public int TotalAccounts { get; set; }
    public int ProcessedCount { get; set; }
    public int QueuedForRetryCount { get; set; }
    public decimal TotalChargesAmount { get; set; }
    public DateRange DateRange { get; set; }
}
