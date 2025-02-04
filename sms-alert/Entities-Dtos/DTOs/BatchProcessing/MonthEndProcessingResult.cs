namespace Entities_Dtos.DTOs.BatchProcessing;

public class MonthEndProcessingResult
{
    public DateTime ProcessingDate { get; set; }
    public int AccountsProcessed { get; set; }
    public decimal TotalConsolidatedAmount { get; set; }
    public int TransactionsConsolidated { get; set; }
}
