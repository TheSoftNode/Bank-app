using Entities_Dtos.Types;

namespace Entities_Dtos.Responses;

public record TransactionResponseDto
{
    public Guid TransactionId { get; init; }
    public string AccountNumber { get; init; }
    public decimal Amount { get; init; }
    public TransactionType TransactionType { get; init; }
    public string TransactionReference { get; init; }
    public string OriginalTransactionReference { get; init; }
    public string Narration { get; init; }
    public DateTime ProcessedDate { get; init; }
    public decimal NewBalance { get; init; }
}
