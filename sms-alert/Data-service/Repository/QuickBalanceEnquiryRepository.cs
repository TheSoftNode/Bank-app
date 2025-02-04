using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.DTOs.BalanceEnquiryDTOs;
using Entities_Dtos.Types;
using Entities_Dtos.Responses;
using Entities_Dtos.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class QuickBalanceEnquiryRepository : GenericRepository<QuickBalanceEnquiry>, IQuickBalanceEnquiryRepository
{
    private readonly IDirectDebitQueueRepository _debitQueueRepository;

    public QuickBalanceEnquiryRepository(
        SMSAlertDbContext context,
        ILogger<QuickBalanceEnquiryRepository> logger,
        IDirectDebitQueueRepository debitQueueRepository) : base(context, logger)
    {
        _debitQueueRepository = debitQueueRepository;
    }

    public async Task<bool> CreateEnquiryAsync(QuickBalanceEnquiryDto enquiryDto)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Email == enquiryDto.Email);

            if (customer == null) return false;

            var account = customer.Accounts
                .FirstOrDefault(a => a.AccountNumber == enquiryDto.AccountNumber);

            if (account == null) return false;

            var enquiry = new QuickBalanceEnquiry
            {
                CustomerId = customer.Id,
                CustomerAccountId = account.Id,
                ChargeAmount = AccountingConstants.QBE_CHARGE,
                SessionCharge = AccountingConstants.TELCO_SESSION_CHARGE,
                TelcoProvider = Enum.Parse<TelcoProvider>(enquiryDto.TelcoProvider),
                IsCharged = false
            };

            await dbSet.AddAsync(enquiry);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateEnquiryAsync method error", typeof(QuickBalanceEnquiryRepository));
            return false;
        }
    }

    public async Task<QuickBalanceEnquiry> GetLatestEnquiryByCustomerAsync(string email)
    {
        try
        {
            return await dbSet
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .Where(q => q.Customer.Email == email)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetLatestEnquiryByCustomerAsync method error", typeof(QuickBalanceEnquiryRepository));
            return null;
        }
    }

    public async Task<IEnumerable<QuickBalanceEnquiry>> GetUnchargedEnquiriesAsync()
    {
        try
        {
            return await dbSet
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .Where(q => !q.IsCharged)
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetUnchargedEnquiriesAsync method error", typeof(QuickBalanceEnquiryRepository));
            return new List<QuickBalanceEnquiry>();
        }
    }

    public async Task<QuickBalanceEnquiryResponse> ProcessEnquiryChargeAsync(Guid enquiryId)
    {
        try
        {
            var enquiry = await dbSet
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .FirstOrDefaultAsync(q => q.Id == enquiryId);

            if (enquiry == null) return null;

            // Create SMS Alert for the QBE charge
            var alert = new SMSAlert
            {
                CustomerId = enquiry.CustomerId,
                CustomerAccountId = enquiry.CustomerAccountId,
                AlertType = AlertType.QuickBalanceEnquiry,
                ChargeAmount = enquiry.ChargeAmount,
                VATAmount = enquiry.ChargeAmount * AccountingConstants.VAT_RATE,
                DeliveryStatus = DeliveryStatus.Delivered,
                MessageContent = $"QBE charge for account {enquiry.Account.AccountNumber}",
                DeliveryTimestamp = DateTime.UtcNow
            };

            _context.SMSAlerts.Add(alert);
            enquiry.IsCharged = true;
            await _context.SaveChangesAsync();

            // Queue the debit request
            await _debitQueueRepository.QueueDebitRequestAsync(alert);

            return new QuickBalanceEnquiryResponse
            {
                EnquiryId = enquiry.Id,
                CustomerNumber = enquiry.Customer.Email,
                AccountNumber = enquiry.Account.AccountNumber,
                Balance = enquiry.Account.Balance,
                ChargeAmount = enquiry.ChargeAmount,
                SessionCharge = enquiry.SessionCharge,
                IsCharged = enquiry.IsCharged
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessEnquiryChargeAsync method error", typeof(QuickBalanceEnquiryRepository));
            throw;
        }
    }

    public async Task<decimal> GetTotalQBEChargesAsync(string email, DateTime startDate, DateTime endDate)
    {
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null) return 0;

            return await dbSet
                .Where(q => q.CustomerId == customer.Id &&
                       q.CreatedAt >= startDate &&
                       q.CreatedAt <= endDate &&
                       q.IsCharged)
                .SumAsync(q => q.ChargeAmount + q.SessionCharge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetTotalQBEChargesAsync method error", typeof(QuickBalanceEnquiryRepository));
            return 0;
        }
    }


    public async Task<ApiResponse<List<QBECustomerSummary>>> GetCustomersForBillingAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var customers = await dbSet
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .Where(q => q.CreatedAt >= startDate &&
                           q.CreatedAt <= endDate &&
                           !q.IsCharged)
                .GroupBy(q => new { q.CustomerId, q.CustomerAccountId })
                .Select(g => new QBECustomerSummary
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerNumber = g.First().Customer.Email,
                    AccountId = g.Key.CustomerAccountId,
                    AccountNumber = g.First().Account.AccountNumber,
                    TotalCharges = g.Sum(q => q.ChargeAmount + q.SessionCharge),
                    TransactionCount = g.Count(),
                    LastTransactionDate = g.Max(q => q.CreatedAt)
                })
                .ToListAsync();

            return new ApiResponse<List<QBECustomerSummary>>
            {
                Success = true,
                Message = "QBE customers retrieved successfully",
                Data = customers,
                Length = customers.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetCustomersForBillingAsync method error", typeof(QuickBalanceEnquiryRepository));
            return new ApiResponse<List<QBECustomerSummary>>
            {
                Success = false,
                Message = "Error retrieving QBE customers",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<List<QuickBalanceEnquiry>>> GetUnchargedEnquiriesForAccountAsync(Guid accountId)
    {
        try
        {
            var enquiries = await dbSet
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .Where(q => q.CustomerAccountId == accountId && !q.IsCharged)
                .ToListAsync();

            return new ApiResponse<List<QuickBalanceEnquiry>>
            {
                Success = true,
                Message = "Uncharged enquiries retrieved successfully",
                Data = enquiries,
                Length = enquiries.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetUnchargedEnquiriesForAccountAsync method error", typeof(QuickBalanceEnquiryRepository));
            return new ApiResponse<List<QuickBalanceEnquiry>>
            {
                Success = false,
                Message = "Error retrieving uncharged enquiries",
                Errors = new List<string> { ex.Message }
            };
        }
    }

}
