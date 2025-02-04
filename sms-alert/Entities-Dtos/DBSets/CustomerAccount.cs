using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets
{
    public class CustomerAccount : BaseEntity
    {
        public string AccountNumber { get; set; }
        public Guid CustomerId { get; set; }
        public AccountType AccountType { get; set; }
        public CurrencyType CurrencyType { get; set; }
        public string BranchSolId { get; set; }  // SOLID of the domiciled account
        public decimal Balance { get; set; }
        public bool IsDomiciliaryAccount { get; set; }
        public string? LinkedNigerianAccountNumber { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public List<AccountTransaction> Transactions { get; set; } = new();
    }
}
