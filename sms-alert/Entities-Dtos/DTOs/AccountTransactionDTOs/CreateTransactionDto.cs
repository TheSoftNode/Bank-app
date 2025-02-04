using Entities_Dtos.Types;

namespace Entities_Dtos.DTOs.AccountTransactionDTOs;

public record CreateTransactionDto
{
    public string AccountNumber { get; init; }
    public decimal Amount { get; init; }
    public TransactionType TransactionType { get; init; }
    public string TransactionReference { get; init; }
    public string OriginalTransactionReference { get; init; }
}
