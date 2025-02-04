using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Entities_Dtos.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class SMSAlertRepository : GenericRepository<SMSAlert>, ISMSAlertRepository
{
    private readonly IDirectDebitQueueRepository _debitQueueRepository;

    public SMSAlertRepository(
        SMSAlertDbContext context,
        ILogger<SMSAlertRepository> logger,
        IDirectDebitQueueRepository debitQueueRepository) : base(context, logger)
    {
        _debitQueueRepository = debitQueueRepository;
    }

    public async Task<bool> CreateAlertAsync(CreateSMSAlertDto alertDto)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Email == alertDto.Email);

            if (customer == null) return false;

            var account = customer.Accounts
                .FirstOrDefault(a => a.AccountNumber == alertDto.AccountNumber);

            var alert = new SMSAlert
            {
                CustomerId = customer.Id,
                CustomerAccountId = account?.Id,
                MessageContent = alertDto.MessageContent,
                AlertType = Enum.Parse<AlertType>(alertDto.AlertType),
                ChargeAmount = AccountingConstants.SMS_ALERT_CHARGE,
                VATAmount = AccountingConstants.SMS_ALERT_CHARGE *
                           AccountingConstants.VAT_RATE,
                DeliveryStatus = DeliveryStatus.Pending,
                DeliveryTimestamp = DateTime.UtcNow
            };

            await dbSet.AddAsync(alert);
            await _context.SaveChangesAsync();

            // Queue the debit request
            if (alert.Id != Guid.Empty)
            {
                await _debitQueueRepository.QueueDebitRequestAsync(alert);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateAlertAsync method error", typeof(SMSAlertRepository));
            return false;
        }
    }


    public async Task<SMSAlert> GetLatestAlertByCustomerAsync(string Email)
    {
        try
        {
            return await dbSet
                .Include(a => a.Customer)
                .Include(a => a.Account)
                .Where(a => a.Customer.Email == Email)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetLatestAlertByCustomerAsync method error", typeof(SMSAlertRepository));
            return null;
        }
    }

    public async Task<SMSAlertResponse> GetAlertStatusAsync(Guid alertId)
    {
        try
        {
            var alert = await dbSet
                .Include(a => a.Customer)
                .Include(a => a.Account)
                .Include(a => a.DebitQueue)
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null) return null;

            return new SMSAlertResponse
            {
                AlertId = alert.Id,
                Email = alert.Customer.Email,
                AccountNumber = alert.Account?.AccountNumber,
                DeliveryStatus = alert.DeliveryStatus.ToString(),
                ChargeAmount = alert.ChargeAmount,
                VatAmount = alert.VATAmount,
                DeliveryTimestamp = alert.DeliveryTimestamp,
                IsCharged = alert.IsCharged
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAlertStatusAsync method error", typeof(SMSAlertRepository));
            return null;
        }
    }

    public async Task<IEnumerable<SMSAlert>> GetUnchargedAlertsAsync()
    {
        try
        {
            return await dbSet
                .Include(a => a.Customer)
                .Include(a => a.Account)
                .Where(a => !a.IsCharged &&
                       a.DeliveryStatus == DeliveryStatus.Delivered)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetUnchargedAlertsAsync method error", typeof(SMSAlertRepository));
            return new List<SMSAlert>();
        }
    }

    public async Task<IEnumerable<SMSAlert>> GetAlertsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            return await dbSet
                .Include(a => a.Customer)
                .Include(a => a.Account)
                .Include(a => a.DebitQueue)
                .Where(a => a.DeliveryTimestamp >= startDate &&
                       a.DeliveryTimestamp <= endDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAlertsByDateRangeAsync method error", typeof(SMSAlertRepository));
            return new List<SMSAlert>();
        }
    }

    public async Task<bool> UpdateDeliveryStatusAsync(Guid alertId, DeliveryStatus status)
    {
        try
        {
            var alert = await dbSet.FindAsync(alertId);
            if (alert == null) return false;

            alert.DeliveryStatus = status;
            alert.UpdatedAt = DateTime.UtcNow;

            _context.Update(alert);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateDeliveryStatusAsync method error", typeof(SMSAlertRepository));
            return false;
        }
    }

    public async Task<decimal> GetTotalChargesForPeriodAsync(string Email, DateTime startDate, DateTime endDate)
    {
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == Email);

            if (customer == null) return 0;

            return await dbSet
                .Where(a => a.CustomerId == customer.Id &&
                       a.DeliveryTimestamp >= startDate &&
                       a.DeliveryTimestamp <= endDate &&
                       a.IsCharged)
                .SumAsync(a => a.ChargeAmount + a.VATAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetTotalChargesForPeriodAsync method error", typeof(SMSAlertRepository));
            return 0;
        }
    }
}