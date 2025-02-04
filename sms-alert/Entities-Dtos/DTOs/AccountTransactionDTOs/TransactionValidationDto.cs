namespace Entities_Dtos.DTOs.AccountTransactionDTOs;

public record TransactionValidationDto
{
    public string TransactionReference { get; init; }
    public bool IsValid { get; init; }
    public string ValidationMessage { get; init; }
}

