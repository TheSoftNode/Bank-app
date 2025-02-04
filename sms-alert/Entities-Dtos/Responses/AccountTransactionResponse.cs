namespace Entities_Dtos.Responses;

public record AccountTransactionResponseDto
{
    public string TransactionReference { get; init; }
    public decimal Amount { get; init; }
    public string TransactionType { get; init; }
    public string Narration { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ProcessedDate { get; init; }
    public string? OriginalTransactionReference { get; init; }
    public bool IsReversed { get; init; }
    public string? ReversalReference { get; init; }
    public string? AccountNumber { get; init; }
    public decimal? Balance { get; init; }
}
