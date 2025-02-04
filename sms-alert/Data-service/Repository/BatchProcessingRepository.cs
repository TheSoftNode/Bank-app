using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.Constants;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs.DirectDebitDTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entities_Dtos.DTOs.BalanceEnquiryDTOs;
using Entities_Dtos.DTOs.BatchProcessing;

namespace Data_service.Repository;

public class BatchProcessingRepository : IBatchProcessingRepository
{
    private readonly SMSAlertDbContext _context;
    private readonly ILogger<BatchProcessingRepository> _logger;
    private readonly IDirectDebitQueueRepository _debitQueueRepository;
    private readonly IAccountingEntryRepository _accountingEntryRepository;
    private readonly ISystemConfigurationRepository _configRepository;

    public BatchProcessingRepository(
        SMSAlertDbContext context,
        ILogger<BatchProcessingRepository> logger,
        IDirectDebitQueueRepository debitQueueRepository,
        IAccountingEntryRepository accountingEntryRepository,
        ISystemConfigurationRepository configRepository)
    {
        _context = context;
        _logger = logger;
        _debitQueueRepository = debitQueueRepository;
        _accountingEntryRepository = accountingEntryRepository;
        _configRepository = configRepository;
    }

    public async Task<ApiResponse<ProcessingResult>> ProcessDailyChargesAsync()
    {
        try
        {
            _logger.LogInformation("Starting daily charges processing");
            var processedSMSCount = 0;
            var processedQBECount = 0;

            // Process pending SMS Alerts from DirectDebitQueue
            var pendingSMSAlerts = await _context.DirectDebitQueues
                .Include(d => d.SMSAlert)
                    .ThenInclude(a => a.Customer)
                .Include(d => d.SMSAlert)
                    .ThenInclude(a => a.Account)
                .Include(d => d.SourceAccount)
                .Where(d => (d.Status == QueueStatus.Pending || d.Status == QueueStatus.Failed) &&
                                d.SMSAlert != null)
                .ToListAsync();

            _logger.LogInformation($"Found {pendingSMSAlerts.Count} pending SMS alerts in queue");

            foreach (var queueItem in pendingSMSAlerts)
            {
                if (queueItem.SourceAccount.Balance >= queueItem.TotalChargeAmount)
                {
                    // Create accounting entry
                    var entryResult = await _accountingEntryRepository.CreateSMSAlertEntryAsync(queueItem.SMSAlert);

                    if (entryResult)
                    {
                        // Deduct from account balance
                        queueItem.SourceAccount.Balance -= queueItem.TotalChargeAmount;
                        queueItem.Status = QueueStatus.Completed;
                        queueItem.SMSAlert.IsCharged = true;
                        queueItem.SMSAlert.DeliveryStatus = DeliveryStatus.Delivered;
                        queueItem.ProcessedDate = DateTime.UtcNow;

                        // Send SMS notification
                        await SendSMSNotification(
                            queueItem.SMSAlert.Customer.PhoneNumber,
                            $"Your account {MaskAccountNumber(queueItem.SourceAccount.AccountNumber)} has been debited with NGN {queueItem.TotalChargeAmount:N2} for SMS Alert service."
                        );

                        processedSMSCount++;
                        _logger.LogInformation($"Successfully processed SMS Alert {queueItem.SMSAlert.Id}. Charged: {queueItem.TotalChargeAmount:C}");
                    }
                    else
                    {
                        queueItem.RetryCount++;
                        queueItem.LastRetryDate = DateTime.UtcNow;
                        queueItem.FailureReason = "Failed to create accounting entry";
                        _logger.LogWarning($"Failed to create accounting entry for SMS Alert {queueItem.SMSAlert.Id}");
                    }
                }
                else
                {
                    queueItem.RetryCount++;
                    queueItem.LastRetryDate = DateTime.UtcNow;
                    queueItem.Status = QueueStatus.Failed;
                    queueItem.FailureReason = "Insufficient funds";
                    _logger.LogInformation($"Insufficient funds for SMS Alert. Required: {queueItem.TotalChargeAmount:C}, Available: {queueItem.SourceAccount.Balance:C}");
                }
            }

            // Process Quick Balance Enquiries
            var unchargedQBE = await _context.QuickBalanceEnquiries
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .Where(q => !q.IsCharged)
                .ToListAsync();

            _logger.LogInformation($"Found {unchargedQBE.Count} unprocessed QBE requests");

            foreach (var qbe in unchargedQBE)
            {
                if (qbe.Account == null)
                {
                    _logger.LogWarning($"No account found for QBE {qbe.Id}. Skipping processing.");
                    continue;
                }

                var totalCharge = qbe.ChargeAmount + qbe.SessionCharge;

                if (qbe.Account.Balance >= totalCharge)
                {
                    var entryResult = await _accountingEntryRepository.CreateQBEEntryAsync(qbe);

                    if (entryResult)
                    {
                        qbe.Account.Balance -= totalCharge;
                        qbe.IsCharged = true;
                        processedQBECount++;

                        // Create SMS Alert for the QBE charge
                        var smsAlert = new SMSAlert
                        {
                            CustomerId = qbe.CustomerId,
                            CustomerAccountId = qbe.CustomerAccountId,
                            MessageContent = $"Your account {MaskAccountNumber(qbe.Account.AccountNumber)} has been charged NGN {totalCharge:N2} for Quick Balance Enquiry service.",
                            ChargeAmount = qbe.ChargeAmount,
                            VATAmount = qbe.ChargeAmount * AccountingConstants.VAT_RATE,
                            AlertType = AlertType.QuickBalanceEnquiry,
                            DeliveryStatus = DeliveryStatus.Pending
                        };

                        _context.SMSAlerts.Add(smsAlert);

                        // Send immediate balance notification
                        await SendSMSNotification(
                            qbe.Customer.PhoneNumber,
                            $"Your account {MaskAccountNumber(qbe.Account.AccountNumber)} balance is NGN {qbe.Account.Balance:N2}"
                        );

                        _logger.LogInformation($"Successfully processed QBE {qbe.Id}. Charged: {totalCharge:C}");
                    }
                    else
                    {
                        await QueueQBEForRetry(qbe);
                    }
                }
                else
                {
                    await QueueQBEForRetry(qbe);
                }
            }

            await _context.SaveChangesAsync();

            var result = new ProcessingResult
            {
                ProcessedSMSAlerts = processedSMSCount,
                ProcessedQBERequests = processedQBECount,
                ProcessingDate = DateTime.UtcNow
            };

            return new ApiResponse<ProcessingResult>
            {
                Success = true,
                Message = "Daily charges processing completed successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessDailyChargesAsync method error", typeof(BatchProcessingRepository));
            return new ApiResponse<ProcessingResult>
            {
                Success = false,
                Message = "Error processing daily charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<QBEProcessingResult>> ProcessMonthlyQBEChargesAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Starting monthly QBE charges processing");

            // Validate date range
            if (startDate > endDate)
            {
                return new ApiResponse<QBEProcessingResult>
                {
                    Success = false,
                    Message = "Invalid date range",
                    Errors = new List<string> { "Start date must be before end date" }
                };
            }

            // 1. Identify all QBE transactions in the date range
            var qbeTransactions = await _context.QuickBalanceEnquiries
                .Include(q => q.Customer)
                .Include(q => q.Account)
                .Where(q => q.CreatedAt >= startDate &&
                           q.CreatedAt <= endDate &&
                           !q.IsCharged)
                .ToListAsync();

            _logger.LogInformation($"Found {qbeTransactions.Count} QBE transactions in date range");

            // 2. Group by customer account and calculate total charges
            var accountCharges = qbeTransactions
                .GroupBy(q => new { q.CustomerAccountId, q.Account.AccountNumber })
                .Select(g => new QBEAccountCharge
                {
                    CustomerAccountId = g.Key.CustomerAccountId,
                    AccountNumber = g.Key.AccountNumber,
                    TotalCharges = g.Sum(q => q.ChargeAmount + q.SessionCharge),
                    TransactionCount = g.Count()
                })
                .ToList();

            var processedCount = 0;
            var queuedCount = 0;

            // 3. Process each account's charges
            foreach (var charge in accountCharges)
            {
                var account = await _context.CustomerAccounts
                    .Include(a => a.Customer)
                    .FirstOrDefaultAsync(a => a.AccountNumber == charge.AccountNumber);

                if (account == null)
                {
                    _logger.LogWarning($"Account {charge.AccountNumber} not found. Skipping processing.");
                    continue;
                }

                if (account.Balance >= charge.TotalCharges)
                {
                    // Create accounting entry
                    var entryResult = await _accountingEntryRepository.CreateQBEEntryAsync(new QuickBalanceEnquiry
                    {
                        CustomerId = account.CustomerId,
                        CustomerAccountId = account.Id,
                        ChargeAmount = charge.TotalCharges,
                        SessionCharge = 0, // Already included in TotalCharges
                        IsCharged = true
                    });

                    if (entryResult)
                    {
                        // Update account balance
                        account.Balance -= charge.TotalCharges;
                        processedCount++;

                        // Mark related QBE requests as charged
                        var relatedQBEs = qbeTransactions
                            .Where(q => q.CustomerAccountId == charge.CustomerAccountId);
                        foreach (var qbe in relatedQBEs)
                        {
                            qbe.IsCharged = true;
                        }

                        // Send notification
                        await SendSMSNotification(
                            account.Customer.PhoneNumber,
                            $"Your account {MaskAccountNumber(account.AccountNumber)} has been debited with NGN {charge.TotalCharges:N2} for QBE service charges."
                        );

                        _logger.LogInformation($"Successfully processed charges for account {account.AccountNumber}. Amount: {charge.TotalCharges:C}");
                    }
                    else
                    {
                        await QueueForRetry(account, charge);
                        queuedCount++;
                    }
                }
                else
                {
                    await QueueForRetry(account, charge);
                    queuedCount++;
                    _logger.LogInformation($"Insufficient funds for account {account.AccountNumber}. Required: {charge.TotalCharges:C}, Available: {account.Balance:C}");
                }
            }

            await _context.SaveChangesAsync();

            var result = new QBEProcessingResult
            {
                ProcessingDate = DateTime.UtcNow,
                TotalAccounts = accountCharges.Count,
                ProcessedCount = processedCount,
                QueuedForRetryCount = queuedCount,
                TotalChargesAmount = accountCharges.Sum(a => a.TotalCharges),
                DateRange = new DateRange
                {
                    StartDate = startDate,
                    EndDate = endDate
                }
            };

            return new ApiResponse<QBEProcessingResult>
            {
                Success = true,
                Message = "Monthly QBE charges processing completed successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessMonthlyQBEChargesAsync method error", typeof(BatchProcessingRepository));
            return new ApiResponse<QBEProcessingResult>
            {
                Success = false,
                Message = "Error processing monthly QBE charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task QueueForRetry(CustomerAccount account, QBEAccountCharge charge)
    {
        var debitQueue = new DirectDebitQueue
        {
            CustomerId = account.CustomerId,
            SourceAccountId = account.Id,
            TotalChargeAmount = charge.TotalCharges,
            Status = QueueStatus.Pending,
            RetryCount = 0,
            TransactionReference = $"QBE_RETRY_{DateTime.UtcNow:yyyyMMdd}_{account.AccountNumber}",
            FailureReason = "Insufficient funds or processing error"
        };

        await _debitQueueRepository.AddAsync(debitQueue);
        _logger.LogInformation($"Queued QBE charges for retry: Account {account.AccountNumber}, Amount: {charge.TotalCharges:C}");
    }

    private string MaskAccountNumber(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
            return accountNumber;

        return $"*****{accountNumber[^4..]}";
    }

    private async Task SendSMSNotification(string phoneNumber, string message)
    {
        // Implement your SMS sending logic here
        // This could call your SMS service provider's API
        _logger.LogInformation($"SMS sent to {phoneNumber}: {message}");
    }

    private async Task QueueQBEForRetry(QuickBalanceEnquiry qbe)
    {
        var debitRequest = new DirectDebitQueue
        {
            CustomerId = qbe.CustomerId,
            SourceAccountId = qbe.CustomerAccountId,
            SMSAlertId = qbe.Id,
            TotalChargeAmount = qbe.ChargeAmount + qbe.SessionCharge,
            Status = QueueStatus.Pending,
            TransactionReference = $"QBE_RETRY_{qbe.Id}",
            FailureReason = "Insufficient funds"
        };

        await _debitQueueRepository.AddAsync(debitRequest);
        _logger.LogInformation($"Queued QBE {qbe.Id} for retry");
    }

    public async Task<bool> ProcessRetryQueueAsync()
    {
        try
        {
            // Get dynamic configuration values
            var maxRetryStr = await _configRepository.GetConfigValueAsync(
                ConfigurationKeys.MAX_RETRY_ATTEMPTS,
                ConfigurationKeys.DEFAULT_MAX_RETRY);

            var retryHoursStr = await _configRepository.GetConfigValueAsync(
                ConfigurationKeys.RETRY_INTERVAL_HOURS,
                ConfigurationKeys.DEFAULT_RETRY_HOURS);

            var maxRetry = int.Parse(maxRetryStr);
            var retryHours = int.Parse(retryHoursStr);

            _logger.LogInformation($"Processing retry queue with maxRetry: {maxRetry}, retryHours: {retryHours}");

            var failedDebits = await _context.DirectDebitQueues
                .Include(d => d.Customer)
                .Include(d => d.SourceAccount)
                .Include(d => d.SMSAlert)
                .Where(d => d.Status == QueueStatus.Failed &&
                       d.RetryCount < maxRetry &&
                       (d.LastRetryDate == null ||
                        EF.Functions.DateDiffHour(d.LastRetryDate, DateTime.UtcNow) >= retryHours))
                .ToListAsync();

            _logger.LogInformation($"Found {failedDebits.Count} failed debits eligible for retry");

            foreach (var debit in failedDebits)
            {
                if (await CheckSufficientBalance(debit.SourceAccount.Id, debit.TotalChargeAmount))
                {
                    debit.Status = QueueStatus.Pending;
                    debit.LastRetryDate = DateTime.UtcNow;
                    debit.RetryCount++;
                    debit.FailureReason = null;
                    _logger.LogInformation($"Requeuing debit {debit.Id} for processing. Retry attempt: {debit.RetryCount}");
                }
                else
                {
                    _logger.LogInformation($"Insufficient balance for debit {debit.Id}. Will retry later.");
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessRetryQueueAsync method error", typeof(BatchProcessingRepository));
            return false;
        }
    }

    public async Task<ApiResponse<TelcoSettlementResult>> ProcessTelcoSettlementsAsync()
    {
        try
        {
            _logger.LogInformation("Starting telco settlements processing");

            // Get all charged QBE transactions grouped by telco
            var telcoCharges = await _context.QuickBalanceEnquiries
                .Include(q => q.Customer)
                .Where(q => q.IsCharged && !q.IsSettled) 
                .GroupBy(q => q.TelcoProvider)
                .Select(g => new
                {
                    Provider = g.Key,
                    TotalSessionCharge = g.Sum(q => q.SessionCharge),
                    CustomerId = g.FirstOrDefault().CustomerId,
                    Transactions = g.ToList(),
                })
                .ToListAsync();

            _logger.LogInformation($"Found {telcoCharges.Count} telco providers to settle");

            if (!telcoCharges.Any())
            {
                return new ApiResponse<TelcoSettlementResult>
                {
                    Success = true,
                    Message = "No pending telco settlements found",
                    Data = new TelcoSettlementResult
                    {
                        SettlementDate = DateTime.UtcNow,
                        ProvidersProcessed = 0,
                        TotalSettlementAmount = 0
                    }
                };
            }

            decimal totalSettlementAmount = 0;
            foreach (var telco in telcoCharges)
            {
                // Create settlement entry from suspense to telco account
                var settlementEntry = new AccountingEntry
                {
                    CustomerId = telco.CustomerId,
                    TransactionReference = $"TELCO_SETTLEMENT_{DateTime.UtcNow:yyyyMMdd}_{telco.Provider}",
                    DebitAmount = telco.TotalSessionCharge,
                    CreditAmount = telco.TotalSessionCharge,
                    DebitAccountNumber = GetTelcoSuspenseAccount(telco.Provider),
                    CreditAccountNumber = GetTelcoSettlementAccount(telco.Provider),
                    VATAccountNumber = GetTelcoSettlementAccount(telco.Provider),
                    Narration = $"Monthly Telco Settlement for {telco.Provider}",
                    EntryType = EntryType.TelcoSessionCharge,
                    ProcessedBy = "SYSTEM",
                    ProcessedDate = DateTime.UtcNow
                };

                await _accountingEntryRepository.AddAsync(settlementEntry);

                // Mark transactions as settled
                foreach (var transaction in telco.Transactions)
                {
                    transaction.IsSettled = true;
                    transaction.SettlementDate = DateTime.UtcNow;
                }

                totalSettlementAmount += telco.TotalSessionCharge;
                _logger.LogInformation($"Created settlement entry for {telco.Provider}: {telco.TotalSessionCharge:C}");
            }

            await _context.SaveChangesAsync();

            var result = new TelcoSettlementResult
            {
                SettlementDate = DateTime.UtcNow,
                ProvidersProcessed = telcoCharges.Count,
                TotalSettlementAmount = totalSettlementAmount
            };

            return new ApiResponse<TelcoSettlementResult>
            {
                Success = true,
                Message = $"Successfully processed settlements for {telcoCharges.Count} telco providers with total amount NGN {totalSettlementAmount:N2}",
                Data = result,
                Length = telcoCharges.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessTelcoSettlementsAsync method error", typeof(BatchProcessingRepository));
            return new ApiResponse<TelcoSettlementResult>
            {
                Success = false,
                Message = "Error processing telco settlements",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<ReconciliationResult>> ReconcileFailedTransactionsAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation($"Starting failed transactions reconciliation for {date:yyyy-MM-dd}");

            var maxRetryStr = await _configRepository.GetConfigValueAsync(
                ConfigurationKeys.MAX_RETRY_ATTEMPTS,
                ConfigurationKeys.DEFAULT_MAX_RETRY);
            var maxRetry = int.Parse(maxRetryStr);

            var failedTransactions = await _context.DirectDebitQueues
                .Include(d => d.SMSAlert)
                .Include(d => d.SourceAccount)
                .Where(d => d.Status == QueueStatus.Failed &&
                       d.CreatedAt.Date == date.Date &&
                       d.RetryCount >= maxRetry)
                .ToListAsync();

            _logger.LogInformation($"Found {failedTransactions.Count} failed transactions to reconcile");

            if (!failedTransactions.Any())
            {
                return new ApiResponse<ReconciliationResult>
                {
                    Success = true,
                    Message = $"No failed transactions found requiring reconciliation for {date:dd MMM yyyy}",
                    Data = new ReconciliationResult
                    {
                        ProcessedDate = DateTime.UtcNow,
                        TransactionsProcessed = 0,
                        ConsolidatedAmount = 0
                    }
                };
            }

            // Group failed transactions by account for consolidation
            var consolidatedCharges = failedTransactions
                .GroupBy(t => new { t.CustomerId, t.SourceAccountId })
                .Select(g => new
                {
                    CustomerId = g.Key.CustomerId,
                    SourceAccountId = g.Key.SourceAccountId,
                    TotalAmount = g.Sum(t => t.TotalChargeAmount),
                    Transactions = g.ToList()
                })
                .ToList();

            foreach (var consolidated in consolidatedCharges)
            {
                // Create new debit queue entry for next month
                var newQueueEntry = new DirectDebitQueue
                {
                    CustomerId = consolidated.CustomerId,
                    SourceAccountId = consolidated.SourceAccountId,
                    TotalChargeAmount = consolidated.TotalAmount,
                    Status = QueueStatus.Pending,
                    RetryCount = 0,
                    TransactionReference = $"CONSOL_{DateTime.UtcNow:yyyyMM}_{consolidated.SourceAccountId}",
                    FailureReason = "Consolidated failed charges from previous month",
                    CreatedAt = new DateTime(date.Year, date.Month + 1, 1)  // First day of next month
                };

                await _context.DirectDebitQueues.AddAsync(newQueueEntry);

                // Process existing failed transactions
                foreach (var transaction in consolidated.Transactions)
                {
                    if (transaction.SMSAlert != null)
                    {
                        var reversalEntry = new AccountingEntry
                        {
                            CustomerId = transaction.CustomerId,
                            TransactionReferenceId = transaction.Id,
                            TransactionReference = $"REV_{transaction.TransactionReference}",
                            DebitAmount = 0,
                            CreditAmount = transaction.TotalChargeAmount,
                            DebitAccountNumber = transaction.SourceAccount.AccountNumber,
                            CreditAccountNumber = AccountingConstants.AccountNumbers.SMS_ALERT_INCOME,
                            VATAmount = transaction.TotalChargeAmount * AccountingConstants.VAT_RATE,
                            Narration = $"Reversal: Failed SMS Alert Charge for {transaction.SourceAccount.AccountNumber}",
                            EntryType = EntryType.SMSAlertCharge,
                            ProcessedBy = "SYSTEM",
                            ProcessedDate = DateTime.UtcNow
                        };

                        await _accountingEntryRepository.AddAsync(reversalEntry);
                    }

                    transaction.Status = QueueStatus.Completed;
                    transaction.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Failed transactions reconciliation completed successfully");

            var result = new ReconciliationResult
            {
                ProcessedDate = DateTime.UtcNow,
                TransactionsProcessed = failedTransactions.Count,
                ConsolidatedAmount = consolidatedCharges.Sum(c => c.TotalAmount),
                ConsolidatedAccounts = consolidatedCharges.Count
            };

            return new ApiResponse<ReconciliationResult>
            {
                Success = true,
                Message = $"Successfully reconciled {failedTransactions.Count} transactions and consolidated NGN {result.ConsolidatedAmount:N2} for {result.ConsolidatedAccounts} accounts",
                Data = result,
                Length = failedTransactions.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ReconcileFailedTransactionsAsync method error", typeof(BatchProcessingRepository));
            return new ApiResponse<ReconciliationResult>
            {
                Success = false,
                Message = "Error reconciling failed transactions",
                Errors = new List<string> { ex.Message }
            };
        }
    }
    public async Task<ApiResponse<MonthEndProcessingResult>> ProcessMonthEndChargesAsync(DateTime monthEndDate)
    {
        try
        {
            _logger.LogInformation($"Starting month-end charges processing for {monthEndDate:yyyy-MM}");

            var startDate = new DateTime(monthEndDate.Year, monthEndDate.Month, 1);
            var endDate = monthEndDate;

            // Get unprocessed charges for the month
            var unprocessedCharges = await _context.DirectDebitQueues
                .Include(d => d.SMSAlert)
                .Include(d => d.SourceAccount)
                .Include(d => d.Customer)
                .Where(d => d.CreatedAt >= startDate &&
                       d.CreatedAt <= endDate &&
                       d.Status == QueueStatus.Failed)
                .GroupBy(d => new
                {
                    d.CustomerId,
                    d.Customer.Email,
                    AccountId = d.SourceAccount.Id,
                    d.SourceAccount.AccountNumber
                })
                .Select(g => new
                {
                    g.Key.CustomerId,
                    g.Key.Email,
                    g.Key.AccountId,
                    g.Key.AccountNumber,
                    TotalAmount = g.Sum(d => d.TotalChargeAmount),
                    TransactionCount = g.Count()
                })
                .ToListAsync();

            if (!unprocessedCharges.Any())
            {
                return new ApiResponse<MonthEndProcessingResult>
                {
                    Success = true,
                    Message = $"No failed charges found requiring consolidation for {monthEndDate:MMMM yyyy}",
                    Data = new MonthEndProcessingResult
                    {
                        ProcessingDate = monthEndDate,
                        AccountsProcessed = 0,
                        TotalConsolidatedAmount = 0,
                        TransactionsConsolidated = 0
                    }
                };
            }

            decimal totalConsolidatedAmount = 0;
            int totalTransactions = 0;

            foreach (var charge in unprocessedCharges)
            {
                var consolidatedCharge = new DirectDebitQueue
                {
                    CustomerId = charge.CustomerId,
                    SourceAccountId = charge.AccountId,
                    TotalChargeAmount = charge.TotalAmount,
                    Status = QueueStatus.Pending,
                    RetryCount = 0,
                    FailureReason = "Month-end consolidated charge",
                    TransactionReference = $"MONTH_END_{monthEndDate:yyyyMM}_{charge.Email}",
                    CreatedAt = DateTime.UtcNow,
                    ProcessedDate = DateTime.UtcNow
                };

                await _debitQueueRepository.AddAsync(consolidatedCharge);

                totalConsolidatedAmount += charge.TotalAmount;
                totalTransactions += charge.TransactionCount;

                _logger.LogInformation($"Created consolidated charge for customer {charge.Email}: {charge.TotalAmount:C}");
            }

            var result = new MonthEndProcessingResult
            {
                ProcessingDate = monthEndDate,
                AccountsProcessed = unprocessedCharges.Count,
                TotalConsolidatedAmount = totalConsolidatedAmount,
                TransactionsConsolidated = totalTransactions
            };

            return new ApiResponse<MonthEndProcessingResult>
            {
                Success = true,
                Message = $"Successfully consolidated {totalTransactions} failed charges totaling NGN {totalConsolidatedAmount:N2} for {unprocessedCharges.Count} accounts",
                Data = result,
                Length = unprocessedCharges.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessMonthEndChargesAsync method error", typeof(BatchProcessingRepository));
            return new ApiResponse<MonthEndProcessingResult>
            {
                Success = false,
                Message = "Error processing month-end charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<bool> CheckSufficientBalance(Guid accountId, decimal amount)
    {
        try
        {
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
            {
                _logger.LogWarning($"Account {accountId} not found during balance check");
                return false;
            }

            return account.Balance >= amount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CheckSufficientBalance method error", typeof(BatchProcessingRepository));
            return false;
        }
    }

    private string GetTelcoSuspenseAccount(TelcoProvider provider)
    {
        return provider switch
        {
            TelcoProvider.MTN => AccountingConstants.AccountNumbers.TelcoSuspense.MTN,
            TelcoProvider.Airtel => AccountingConstants.AccountNumbers.TelcoSuspense.AIRTEL,
            TelcoProvider.Glo => AccountingConstants.AccountNumbers.TelcoSuspense.GLO,
            TelcoProvider.NineMobile => AccountingConstants.AccountNumbers.TelcoSuspense.NINE_MOBILE,
            _ => throw new ArgumentException($"Invalid telco provider: {provider}")
        };
    }

    private string GetTelcoSettlementAccount(TelcoProvider provider)
    {
        return provider switch
        {
            TelcoProvider.MTN => "2004874948",
            TelcoProvider.Airtel => "2002751537",
            TelcoProvider.Glo => "2018285619",
            TelcoProvider.NineMobile => "2012887918",
            _ => throw new ArgumentException($"Invalid telco provider: {provider}")
        };
    }
}
