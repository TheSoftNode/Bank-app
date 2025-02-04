using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets;

public class BatchChargeArchive
{
    public Guid Id { get; set; }
    public required string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string ChargeReason { get; set; }
    public BatchChargeStatus FinalStatus { get; set; }
    public DateTime ProcessedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ArchivedAt { get; set; }
    public string ProcessedBy { get; set; } = "SYSTEM";

    // Accounting details
    public string DebitAccountNumber { get; set; }
    public string CreditAccountNumber { get; set; }
}
