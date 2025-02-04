namespace Entities_Dtos.DTOs.BatchProcessing;

public class ReconciliationResult
{
    public DateTime ProcessedDate { get; set; }
    public int TransactionsProcessed { get; set; }
    public decimal ConsolidatedAmount { get; set; }
    public int ConsolidatedAccounts { get; set; }
}
