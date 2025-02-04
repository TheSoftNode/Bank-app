namespace Entities_Dtos.Responses;

public class CustomerAccountResponse
{
    public Guid AccountId { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountType { get; set; }
    public string? CurrencyType { get; set; }
    public decimal Balance { get; set; }
    public string? BranchSolId { get; set; }
    public string? Email { get; set; }
    public bool IsDomiciliaryAccount { get; set; }
    public string? LinkedNigerianAccountNumber { get; set; }
}

