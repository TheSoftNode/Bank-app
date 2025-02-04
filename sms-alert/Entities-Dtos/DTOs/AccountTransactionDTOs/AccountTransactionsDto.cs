using Entities_Dtos.Responses;

namespace Entities_Dtos.DTOs.AccountTransactionDTOs;

public record AccountTransactionsDto
{
    public string AccountNumber { get; init; }
    public string AccountName { get; init; }
    public int TransactionCount { get; init; }
    public string? DateRange { get; init; }
    public List<AccountTransactionResponseDto> Transactions { get; init; } = new();
}
