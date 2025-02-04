using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(SMSAlertDbContext context, ILogger<CustomerRepository> logger) : base(context, logger)
    {
    }

    public async Task<bool> AnyCustomersExistAsync()
    {
        return await dbSet.AnyAsync();
    }

    public async Task<Customer> GetByEmailAsync(string email)
    {
        return await dbSet
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<ApiResponse<List<Customer>>> GetAllCustomersAsync()
    {
        try
        {
            var customers = await dbSet
                .Include(c => c.Accounts)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            if (!customers.Any())
            {
                return new ApiResponse<List<Customer>>
                {
                    Success = true,
                    Message = "No customers found",
                    Data = new List<Customer>(),
                    Length = 0
                };
            }

            return new ApiResponse<List<Customer>>
            {
                Success = true,
                Message = "Customers retrieved successfully",
                Data = customers,
                Length = customers.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAllCustomersAsync method error", typeof(CustomerRepository));
            return new ApiResponse<List<Customer>>
            {
                Success = false,
                Message = "Error retrieving customers",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<CustomerAccount> GetNairaAccountForDomAccountAsync(string domAccountNumber)
    {
        try
        {
            var domAccount = await _context.CustomerAccounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a =>
                    a.AccountNumber == domAccountNumber &&
                    a.AccountType == AccountType.Domiciliary);

            if (domAccount == null) return null;

            return await _context.CustomerAccounts
                .FirstOrDefaultAsync(a =>
                    a.CustomerId == domAccount.CustomerId &&
                    a.AccountType != AccountType.Domiciliary &&
                    a.CurrencyType == CurrencyType.NGN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetNairaAccountForDomAccountAsync method error", typeof(CustomerRepository));
            return null;
        }
    }

    public async Task<IEnumerable<CustomerAccount>> GetAllAccountsAsync(string email)
    {
        try
        {
            var customer = await GetByEmailAsync(email);
            if (customer == null) return new List<CustomerAccount>();

            return await _context.CustomerAccounts
                .Where(a => a.CustomerId == customer.Id)
                .Include(a => a.Transactions)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAllAccountsAsync method error", typeof(CustomerRepository));
            return new List<CustomerAccount>();
        }
    }

    public async Task<bool> HasSufficientBalanceAsync(string accountNumber, decimal amount)
    {
        try
        {
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null) return false;

            // Include any pending debits in the calculation
            var pendingDebits = await _context.DirectDebitQueues
                .Where(d => d.SourceAccountId == account.Id &&
                       d.Status == QueueStatus.Pending)
                .SumAsync(d => d.TotalChargeAmount);

            return (account.Balance - pendingDebits) >= amount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} HasSufficientBalanceAsync method error", typeof(CustomerRepository));
            return false;
        }
    }
}
