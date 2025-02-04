namespace Entities_Dtos.DTOs.AccountTransactionDTOs;

public record DeleteTransactionsResultDto
{
    public string AccountNumber { get; init; }
    public int TotalTransactions { get; init; }
    public int DeletedTransactions { get; init; }
    public int SkippedTransactions { get; init; }
    public List<string> SkippedTransactionDetails { get; init; } = new();
}
