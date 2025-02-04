using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class DirectDebitQueueRepository : GenericRepository<DirectDebitQueue>, IDirectDebitQueueRepository
{
    public DirectDebitQueueRepository(SMSAlertDbContext context, ILogger<DirectDebitQueueRepository> logger) : base(context, logger)
    {
    }

    public async Task<ApiResponse<DirectDebitQueue>> QueueDebitRequestAsync(SMSAlert alert)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == alert.CustomerId);

            if (customer == null)
            {
                return new ApiResponse<DirectDebitQueue>
                {
                    Success = false,
                    Message = "Customer not found",
                    Data = null,
                    Errors = new List<string> { $"Customer with ID {alert.CustomerId} not found" }
                };
            }

            // Determine source account for debit
            CustomerAccount sourceAccount;

            if (alert.Account?.IsDomiciliaryAccount == true)
            {
                // For domiciliary accounts, get the linked Nigerian account
                if (string.IsNullOrEmpty(alert.Account.LinkedNigerianAccountNumber))
                {
                    return new ApiResponse<DirectDebitQueue>
                    {
                        Success = false,
                        Message = "No linked Nigerian account found",
                        Data = null,
                        Errors = new List<string> { $"Domiciliary account {alert.Account.AccountNumber} has no linked Nigerian account" }
                    };
                }

                sourceAccount = await _context.CustomerAccounts
                    .FirstOrDefaultAsync(a =>
                        a.AccountNumber == alert.Account.LinkedNigerianAccountNumber &&
                        !a.IsDomiciliaryAccount &&
                        a.CurrencyType == CurrencyType.NGN);

                if (sourceAccount == null)
                {
                    return new ApiResponse<DirectDebitQueue>
                    {
                        Success = false,
                        Message = "Linked Nigerian account not found",
                        Data = null,
                        Errors = new List<string> { $"Linked Nigerian account {alert.Account.LinkedNigerianAccountNumber} not found" }
                    };
                }
            }
            else
            {
                // For non-domiciliary accounts, use the alert's account
                sourceAccount = alert.Account;
            }

            if (sourceAccount == null)
            {
                return new ApiResponse<DirectDebitQueue>
                {
                    Success = false,
                    Message = "Source account not found",
                    Data = null,
                    Errors = new List<string> { "No valid source account found for debit" }
                };
            }

            var queueItem = new DirectDebitQueue
            {
                CustomerId = customer.Id,
                SMSAlertId = alert.Id,
                SourceAccountId = sourceAccount.Id,
                TotalChargeAmount = alert.ChargeAmount + alert.VATAmount,
                Status = QueueStatus.Pending,
                RetryCount = 0,
                FailureReason = "",
                TransactionReference = $"SMS_{DateTime.UtcNow:yyyyMMddHHmmss}_{alert.Id}"
            };

            await dbSet.AddAsync(queueItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Queued debit request for customer {CustomerId}, Account: {AccountNumber}, Amount: {Amount}",
                customer.Id,
                sourceAccount.AccountNumber,
                queueItem.TotalChargeAmount);

            return new ApiResponse<DirectDebitQueue>
            {
                Success = true,
                Message = "Debit request queued successfully",
                Data = queueItem
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} QueueDebitRequestAsync method error", typeof(DirectDebitQueueRepository));
            return new ApiResponse<DirectDebitQueue>
            {
                Success = false,
                Message = "Error queuing debit request",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<IEnumerable<DirectDebitQueue>> GetPendingDebitsAsync()
    {
        try
        {
            return await dbSet
                .Include(d => d.Customer)
                .Include(d => d.SMSAlert)
                .Include(d => d.SourceAccount)
                .Where(d => d.Status == QueueStatus.Pending)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetPendingDebitsAsync method error", typeof(DirectDebitQueueRepository));
            return new List<DirectDebitQueue>();
        }
    }

    public async Task<IEnumerable<DirectDebitQueue>> GetFailedDebitsAsync()
    {
        try
        {
            return await dbSet
                .Include(d => d.Customer)
                .Include(d => d.SMSAlert)
                .Include(d => d.SourceAccount)
                .Where(d => d.Status == QueueStatus.Failed &&
                       d.RetryCount < 3)  // Configurable retry limit
                .OrderBy(d => d.LastRetryDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetFailedDebitsAsync method error", typeof(DirectDebitQueueRepository));
            return new List<DirectDebitQueue>();
        }
    }

    public async Task<bool> UpdateQueueStatusAsync(Guid queueId, QueueStatus status, string failureReason = null)
    {
        try
        {
            var queueItem = await dbSet.FindAsync(queueId);
            if (queueItem == null) return false;

            queueItem.Status = status;
            queueItem.FailureReason = failureReason;
            queueItem.UpdatedAt = DateTime.UtcNow;

            if (status == QueueStatus.Failed)
            {
                queueItem.RetryCount++;
                queueItem.LastRetryDate = DateTime.UtcNow;
            }

            _context.Update(queueItem);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateQueueStatusAsync method error", typeof(DirectDebitQueueRepository));
            return false;
        }
    }

    public async Task<bool> RequeueFailedChargesAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var failedItems = await dbSet
                .Where(d => d.Status == QueueStatus.Failed &&
                       d.RetryCount < 3 &&
                       d.CreatedAt >= startDate &&
                       d.CreatedAt <= endDate)
                .ToListAsync();

            foreach (var item in failedItems)
            {
                item.Status = QueueStatus.Pending;
                item.LastRetryDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} RequeueFailedChargesAsync method error", typeof(DirectDebitQueueRepository));
            return false;
        }
    }

    public async Task<int> GetRetryCountAsync(Guid queueId)
    {
        try
        {
            var queueItem = await dbSet.FindAsync(queueId);
            return queueItem?.RetryCount ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetRetryCountAsync method error", typeof(DirectDebitQueueRepository));
            return 0;
        }
    }
}
