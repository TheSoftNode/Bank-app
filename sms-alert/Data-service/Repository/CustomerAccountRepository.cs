using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class CustomerAccountRepository : GenericRepository<CustomerAccount>, ICustomerAccountRepository
{
    public CustomerAccountRepository(
        SMSAlertDbContext context,
        ILogger<CustomerAccountRepository> logger) : base(context, logger)
    {
    }

    public async Task<CustomerAccount> GetByAccountNumberAsync(string accountNumber)
    {
        try
        {
            return await dbSet
                .Include(a => a.Customer)
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetByAccountNumberAsync method error", typeof(CustomerAccountRepository));
            return null;
        }
    }

    public async Task<CustomerAccount> GetByCustomerAndTypeAsync(Guid customerId, AccountType accountType)
    {
        try
        {
            return await dbSet
                .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AccountType == accountType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetByCustomerAndTypeAsync method error", typeof(CustomerAccountRepository));
            return null!;
        }
    }

    public async Task<bool> UpdateBalanceAsync(string accountNumber, decimal amount, bool isCredit)
    {
        try
        {
            var account = await dbSet.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            if (account == null) return false;

            if (isCredit)
                account.Balance += amount;
            else
            {
                if (account.Balance < amount) return false;
                account.Balance -= amount;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateBalanceAsync method error", typeof(CustomerAccountRepository));
            return false;
        }
    }

    public async Task<IEnumerable<CustomerAccount>> GetAccountsByTypeAsync(string email, AccountType accountType)
    {
        try
        {
            return await dbSet
                .Include(a => a.Customer)
                .Where(a => a.Customer.Email == email &&
                           a.AccountType == accountType)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAccountsByTypeAsync method error", typeof(CustomerAccountRepository));
            return new List<CustomerAccount>();
        }
    }

    public async Task<bool> ValidateAccountAsync(string accountNumber, CurrencyType currencyType)
    {
        try
        {
            return await dbSet.AnyAsync(a =>
                a.AccountNumber == accountNumber &&
                a.CurrencyType == currencyType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ValidateAccountAsync method error", typeof(CustomerAccountRepository));
            return false;
        }
    }

    public async Task<string> GetBranchSolIdAsync(string accountNumber)
    {
        try
        {
            var account = await dbSet
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            return account?.BranchSolId!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetBranchSolIdAsync method error", typeof(CustomerAccountRepository));
            return null!;
        }
    }

    public async Task<decimal> GetBalanceAsync(string accountNumber)
    {
        try
        {
            var account = await dbSet
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            return account?.Balance ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetBalanceAsync method error", typeof(CustomerAccountRepository));
            return 0;
        }
    }

    public async Task<IEnumerable<CustomerAccount>> GetAccountsByCustomerIdAsync(Guid customerId)
    {
        try
        {
            return await dbSet
                .Include(a => a.Transactions)
                .Where(a => a.CustomerId == customerId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAccountsByCustomerIdAsync method error", typeof(CustomerAccountRepository));
            return new List<CustomerAccount>();
        }
    }

    public async Task<bool> ValidateNigerianAccountForLinkingAsync(string accountNumber)
    {
        try
        {
            return await dbSet.AnyAsync(a =>
                a.AccountNumber == accountNumber &&
                a.CurrencyType == CurrencyType.NGN &&
                !a.IsDomiciliaryAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ValidateNigerianAccountForLinkingAsync method error", typeof(CustomerAccountRepository));
            return false;
        }
    }

    public async Task<bool> DebitChargesFromLinkedAccountAsync(string domiciliaryAccountNumber, decimal charges)
    {
        try
        {
            // First get the domiciliary account
            var domAccount = await dbSet
                .FirstOrDefaultAsync(a =>
                    a.AccountNumber == domiciliaryAccountNumber &&
                    a.IsDomiciliaryAccount);

            if (domAccount == null || string.IsNullOrEmpty(domAccount.LinkedNigerianAccountNumber))
            {
                _logger.LogWarning("Domiciliary account not found or no linked Nigerian account for account number: {AccountNumber}",
                    domiciliaryAccountNumber);
                return false;
            }

            // Get the linked Nigerian account
            var nigerianAccount = await dbSet
                .FirstOrDefaultAsync(a =>
                    a.AccountNumber == domAccount.LinkedNigerianAccountNumber &&
                    !a.IsDomiciliaryAccount &&
                    a.CurrencyType == CurrencyType.NGN);

            if (nigerianAccount == null)
            {
                _logger.LogWarning("Linked Nigerian account not found: {LinkedAccount}",
                    domAccount.LinkedNigerianAccountNumber);
                return false;
            }

            // Check if the Nigerian account has sufficient balance
            if (nigerianAccount.Balance < charges)
            {
                _logger.LogWarning("Insufficient balance in linked Nigerian account: {LinkedAccount}, Required: {Charges}, Available: {Balance}",
                    nigerianAccount.AccountNumber, charges, nigerianAccount.Balance);
                return false;
            }

            // Debit the Nigerian account
            nigerianAccount.Balance -= charges;

            // Create a transaction record with the correct properties
            var transaction = new AccountTransaction
            {
                CustomerAccountId = nigerianAccount.Id,
                Amount = charges,
                TransactionType = TransactionType.Debit,
                TransactionReference = $"DOM_CHRG_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}",
                Narration = $"Domiciliary Account Charges for {domiciliaryAccountNumber}",
                ProcessedDate = DateTime.UtcNow,
                IsReversed = false,
                OriginalTransactionReference = null,
                ReversalReference = null,
                ReversalDate = null
            };

            await _context.AccountTransactions.AddAsync(transaction);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} DebitChargesFromLinkedAccountAsync method error for account: {AccountNumber}",
                typeof(CustomerAccountRepository), domiciliaryAccountNumber);
            return false;
        }
    }
}
