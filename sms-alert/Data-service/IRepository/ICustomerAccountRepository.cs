using Entities_Dtos.DBSets;
using Entities_Dtos.Types;

namespace Data_service.IRepository;

public interface ICustomerAccountRepository : IGenericRepository<CustomerAccount>
{
    Task<CustomerAccount> GetByAccountNumberAsync(string accountNumber);
    Task<CustomerAccount> GetByCustomerAndTypeAsync(Guid customerId, AccountType accountType);
    Task<bool> UpdateBalanceAsync(string accountNumber, decimal amount, bool isCredit);
    Task<IEnumerable<CustomerAccount>> GetAccountsByTypeAsync(string customerNumber, AccountType accountType);
    Task<bool> ValidateAccountAsync(string accountNumber, CurrencyType currencyType);
    Task<string> GetBranchSolIdAsync(string accountNumber);
    Task<decimal> GetBalanceAsync(string accountNumber);
    Task<IEnumerable<CustomerAccount>> GetAccountsByCustomerIdAsync(Guid customerId);
    Task<bool> ValidateNigerianAccountForLinkingAsync(string accountNumber);
    Task<bool> DebitChargesFromLinkedAccountAsync(string domiciliaryAccountNumber, decimal charges);
}
