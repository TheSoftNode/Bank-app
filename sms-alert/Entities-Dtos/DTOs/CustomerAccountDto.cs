using Entities_Dtos.Types;

namespace Entities_Dtos.DTOs;

public class CustomerAccountDto
{
    public string Email { get; set; }
    public string AccountNumber { get; set; }
    public AccountType AccountType { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public string BranchSolId { get; set; }
    public decimal Balance { get; set; }
    public bool IsDomiciliaryAccount { get; set; }
    public string LinkedNigerianAccountNumber { get; set; }
}
