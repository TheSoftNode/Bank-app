namespace Entities_Dtos.Responses;

public class DirectDebitQueueResponseDto
{
    public Guid QueueId { get; set; }
    public string Email { get; set; }
    public string AccountNumber { get; set; }
    public decimal TotalChargeAmount { get; set; }
    public string Status { get; set; }
    public int RetryCount { get; set; }
    public string FailureReason { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public string TransactionReference { get; set; }
    public DateTime CreatedAt { get; set; }
}
