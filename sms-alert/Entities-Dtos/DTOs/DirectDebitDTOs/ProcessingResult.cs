namespace Entities_Dtos.DTOs.DirectDebitDTOs;

public class ProcessingResult
{
    public int ProcessedSMSAlerts { get; set; }
    public int ProcessedQBERequests { get; set; }
    public DateTime ProcessingDate { get; set; }
}
