namespace Entities_Dtos.Responses;

public class DebitQueueResponse
{
    public Guid QueueId { get; set; }
    public string CustomerNumber { get; set; }
    public string AccountNumber { get; set; }
    public decimal TotalChargeAmount { get; set; }
    public string Status { get; set; }
    public int RetryCount { get; set; }
    public string FailureReason { get; set; }
}
