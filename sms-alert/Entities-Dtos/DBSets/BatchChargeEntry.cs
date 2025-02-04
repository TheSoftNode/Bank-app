namespace Entities_Dtos.DBSets;

using Entities_Dtos.Types;

public class BatchChargeEntry
{
    public Guid Id { get; set; }
    public required string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string ChargeReason { get; set; }
    public BatchChargeStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsProcessed { get; set; }
}
