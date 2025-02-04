using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.Constants;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs.BatchChargeDTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace Data_service.Repository;

public class BatchChargeRepository : IBatchChargeRepository
{
    private readonly SMSAlertDbContext _context;
    private readonly ILogger<BatchChargeRepository> _logger;
    private readonly ISystemConfigurationRepository _configRepository;
    private readonly IAccountingEntryRepository _accountingEntryRepository;

    public BatchChargeRepository(
        SMSAlertDbContext context,
        ILogger<BatchChargeRepository> logger,
        ISystemConfigurationRepository configRepository,
        IAccountingEntryRepository accountingEntryRepository)
    {
        _context = context;
        _logger = logger;
        _configRepository = configRepository;
        _accountingEntryRepository = accountingEntryRepository;
    }

    //public async Task<ApiResponse<BatchChargeResult>> QueueChargesAsync(List<AccountChargeDetail> charges)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Starting batch charge queueing for {Count} charges", charges.Count);

    //        var entries = new List<BatchChargeEntry>();
    //        var now = DateTime.UtcNow;

    //        foreach (var charge in charges)
    //        {
    //            var entry = new BatchChargeEntry
    //            {
    //                AccountNumber = charge.AccountNumber,
    //                Amount = charge.Amount,
    //                ChargeReason = charge.ChargeReason,
    //                Status = BatchChargeStatus.Pending,
    //                RetryCount = 0,
    //                CreatedAt = now,
    //                UpdatedAt = now
    //            };

    //            entries.Add(entry);
    //        }

    //        await _context.BatchCharges.AddRangeAsync(entries);
    //        await _context.SaveChangesAsync();

    //        _logger.LogInformation("Successfully queued {Count} charges for processing", entries.Count);

    //        return new ApiResponse<BatchChargeResult>
    //        {
    //            Success = true,
    //            Message = $"Successfully queued {entries.Count} charges for processing",
    //            Data = new BatchChargeResult
    //            {
    //                TotalCharges = entries.Count,
    //                TotalAmount = entries.Sum(e => e.Amount),
    //                ProcessingDate = now
    //            }
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error queueing batch charges");
    //        return new ApiResponse<BatchChargeResult>
    //        {
    //            Success = false,
    //            Message = "Error queueing batch charges",
    //            Errors = new List<string> { ex.Message }
    //        };
    //    }
    //}

    public async Task<ApiResponse<BatchChargeResult>> QueueChargesAsync(List<AccountChargeDetail> charges, bool processImmediately = false)
    {
        // Validate that amounts are either multiples of 4 or 10
        var invalidCharges = charges.Where(c => !(c.Amount % 4 == 0 || c.Amount % 10 == 0)).ToList();

        if (invalidCharges.Any())
        {
            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Invalid charge amounts detected",
                Errors = invalidCharges.Select(c =>
                    $"Invalid charge amount: {c.Amount}. Amount must be a multiple of 4 for SMS alerts or multiple of 10 for QBE alerts.").ToList()
            };
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Starting batch charge queueing for {Count} charges. ProcessImmediately: {Immediate}",
                charges.Count, processImmediately);

            var entries = new List<BatchChargeEntry>();
            var now = DateTime.UtcNow;

            foreach (var charge in charges)
            {
                string chargeReason = charge.Amount % 10 == 0
                ? "quick balance enquiry alert charge"
                : "sms-alert charge";

                var entry = new BatchChargeEntry
                {
                    AccountNumber = charge.AccountNumber,
                    Amount = charge.Amount,
                    ChargeReason = chargeReason,
                    FailureReason = "NONE",
                    Status = processImmediately ? BatchChargeStatus.Processing : BatchChargeStatus.Pending,
                    RetryCount = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                entries.Add(entry);
            }

            await _context.BatchCharges.AddRangeAsync(entries);
            await _context.SaveChangesAsync();

            var result = new BatchChargeResult
            {
                TotalCharges = entries.Count,
                TotalAmount = entries.Sum(e => e.Amount),
                ProcessingDate = now
            };

            if (processImmediately)
            {
                var processResult = await ProcessPendingChargesInternalAsync();
                if (!processResult.Success)
                {
                    await transaction.RollbackAsync();
                    return processResult;  
                }
                result = processResult.Data;
            }

            await transaction.CommitAsync();

            return new ApiResponse<BatchChargeResult>
            {
                Success = true,
                Message = processImmediately
                    ? $"Successfully processed {result.ProcessedCount} charges, {result.FailedCount} failed"
                    : $"Successfully queued {entries.Count} charges for processing",
                Data = result
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error queueing batch charges");
            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Error queueing batch charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    // Internal method for processing within an existing transaction
    private async Task<ApiResponse<BatchChargeResult>> ProcessPendingChargesInternalAsync()
    {

        try
        {
            _logger.LogInformation("Starting processing of pending batch charges");

            var pendingCharges = await _context.BatchCharges
                .Where(c => c.Status == BatchChargeStatus.Pending || c.Status == BatchChargeStatus.Processing)
                .ToListAsync();

            if (!pendingCharges.Any())
            {
                return new ApiResponse<BatchChargeResult>
                {
                    Success = true,
                    Message = "No pending charges found",
                    Data = new BatchChargeResult
                    {
                        ProcessingDate = DateTime.UtcNow
                    }
                };
            }

            var result = new BatchChargeResult
            {
                TotalCharges = pendingCharges.Count,
                TotalAmount = pendingCharges.Sum(c => c.Amount),
                ProcessingDate = DateTime.UtcNow
            };

            var successfulCharges = new List<BatchChargeEntry>();
            foreach (var charge in pendingCharges)
            {
                var processResult = await ProcessChargeAsync(charge);
                if (processResult.Success)
                {
                    result.ProcessedCount++;
                    result.ProcessedAmount += charge.Amount;
                    successfulCharges.Add(charge);
                }
                else
                {
                    result.FailedCount++;
                    result.FailedAmount += charge.Amount;
                }

                result.Details.Add(new BatchChargeDetail
                {
                    AccountNumber = charge.AccountNumber,
                    Amount = charge.Amount,
                    Status = charge.Status.ToString(),
                    FailureReason = charge.FailureReason
                });
            }

            // Archive successful charges
            if (successfulCharges.Any())
            {
                var archiveEntries = successfulCharges.Select(c => new BatchChargeArchive
                {
                    Id = Guid.NewGuid(),
                    AccountNumber = c.AccountNumber,
                    Amount = c.Amount,
                    ChargeReason = c.ChargeReason,
                    FinalStatus = c.Status,
                    ProcessedDate = c.ProcessedDate ?? DateTime.UtcNow,
                    DebitAccountNumber = c.AccountNumber,
                    CreditAccountNumber = AccountingConstants.AccountNumbers.SMS_ALERT_INCOME,
                    CreatedAt = c.CreatedAt,
                    ArchivedAt = DateTime.UtcNow
                }).ToList();

                await _context.BatchChargeArchives.AddRangeAsync(archiveEntries);
                _context.BatchCharges.RemoveRange(successfulCharges);
            }

            await _context.SaveChangesAsync();

            return new ApiResponse<BatchChargeResult>
            {
                Success = true,
                Message = $"Processed {result.ProcessedCount} charges successfully, {result.FailedCount} failed",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending charges");
            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Error processing pending charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<BatchChargeResult>> ProcessPendingChargesAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Starting processing of pending batch charges");

            var pendingCharges = await _context.BatchCharges
                .Where(c => c.Status == BatchChargeStatus.Pending || c.Status == BatchChargeStatus.Processing)
                .ToListAsync();

            if (!pendingCharges.Any())
            {
                return new ApiResponse<BatchChargeResult>
                {
                    Success = true,
                    Message = "No pending charges found",
                    Data = new BatchChargeResult { ProcessingDate = DateTime.UtcNow }
                };
            }

            var result = new BatchChargeResult
            {
                TotalCharges = pendingCharges.Count,
                TotalAmount = pendingCharges.Sum(c => c.Amount),
                ProcessingDate = DateTime.UtcNow
            };

            var successfulCharges = new List<BatchChargeEntry>();
            foreach (var charge in pendingCharges)
            {
                var processResult = await ProcessChargeAsync(charge);
                if (processResult.Success)
                {
                    result.ProcessedCount++;
                    result.ProcessedAmount += charge.Amount;
                    successfulCharges.Add(charge);
                }
                else
                {
                    result.FailedCount++;
                    result.FailedAmount += charge.Amount;
                }

                result.Details.Add(new BatchChargeDetail
                {
                    AccountNumber = charge.AccountNumber,
                    Amount = charge.Amount,
                    Status = charge.Status.ToString(),
                    FailureReason = charge.FailureReason
                });
            }

            // Archive successful charges
            if (successfulCharges.Any())
            {
                var archiveEntries = successfulCharges.Select(c => new BatchChargeArchive
                {
                    Id = Guid.NewGuid(),
                    AccountNumber = c.AccountNumber,
                    Amount = c.Amount,
                    ChargeReason = c.ChargeReason,
                    FinalStatus = c.Status,
                    ProcessedDate = c.ProcessedDate ?? DateTime.UtcNow,
                    DebitAccountNumber = c.AccountNumber,
                    CreditAccountNumber = AccountingConstants.AccountNumbers.SMS_ALERT_INCOME,
                    CreatedAt = c.CreatedAt,
                    ArchivedAt = DateTime.UtcNow
                }).ToList();

                await _context.BatchChargeArchives.AddRangeAsync(archiveEntries);
                _context.BatchCharges.RemoveRange(successfulCharges);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ApiResponse<BatchChargeResult>
            {
                Success = true,
                Message = $"Processed {result.ProcessedCount} charges successfully, {result.FailedCount} failed",
                Data = result
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing pending charges");
            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Error processing pending charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<BatchChargeResult>> ProcessChargeAsync(BatchChargeEntry charge)
    {
        try
        {
            // Validate amount is multiple of 4 or 10
            if (!(charge.Amount % 4 == 0 || charge.Amount % 10 == 0))
            {
                charge.Status = BatchChargeStatus.Failed;
                charge.FailureReason = $"Invalid charge amount: {charge.Amount}. Amount must be a multiple of 4 for SMS alerts or multiple of 10 for QBE alerts.";
                charge.UpdatedAt = DateTime.UtcNow;
                return new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "Invalid charge amount",
                    Errors = new List<string> { charge.FailureReason }
                };
            }

            // Set correct charge reason based on amount
            charge.ChargeReason = charge.Amount % 10 == 0
                ? "quick balance enquiry alert charge"
                : "sms-alert charge";

            _logger.LogInformation("Processing for account {Account}", charge.AccountNumber);

            // Get account
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == charge.AccountNumber);

            if (account == null)
            {
                charge.Status = BatchChargeStatus.Failed;
                charge.FailureReason = "Account not found";
                charge.UpdatedAt = DateTime.UtcNow;
                _logger.LogWarning("Account {Account} not found", charge.AccountNumber);

                return new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "Account not found",
                    Errors = new List<string> { "Account not found" }
                };
            }

            // Check balance
            if (account.Balance < charge.Amount)
            {
                charge.Status = BatchChargeStatus.Failed;
                charge.FailureReason = "Insufficient funds";
                charge.UpdatedAt = DateTime.UtcNow;
                charge.RetryCount++;
                charge.LastRetryDate = DateTime.UtcNow;

                _logger.LogWarning("Insufficient funds in account {Account} for charge. Required: {Amount}, Available: {Balance}",
                    account.AccountNumber, charge.Amount, account.Balance);

                return new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "Insufficient funds",
                    Errors = new List<string> { "Insufficient funds" }
                };
            }

            var now = DateTime.UtcNow;

            // Create archive entry instead of accounting entry
            var archiveEntry = new BatchChargeArchive
            {
                Id = Guid.NewGuid(),
                AccountNumber = charge.AccountNumber,
                Amount = charge.Amount,
                ChargeReason = charge.ChargeReason,
                FinalStatus = BatchChargeStatus.Completed,
                ProcessedDate = now,
                CreatedAt = charge.CreatedAt,
                ArchivedAt = now,
                ProcessedBy = "SYSTEM",
                DebitAccountNumber = account.AccountNumber,
                CreditAccountNumber = charge.ChargeReason == "sms-alert charge"
    ? AccountingConstants.AccountNumbers.SMS_ALERT_INCOME
    : AccountingConstants.AccountNumbers.QBE_ALERT_INCOME,
            };

            // Update account balance
            account.Balance -= charge.Amount;

            // Update charge status for removal
            charge.Status = BatchChargeStatus.Completed;
            charge.ProcessedDate = now;
            charge.UpdatedAt = now;
            charge.IsProcessed = true;

            // Add to archive
            await _context.BatchChargeArchives.AddAsync(archiveEntry);

            _logger.LogInformation("Successfully processed and charged {Account} for account {ChargeReason}. Amount: {Amount}",
                account.AccountNumber,charge.ChargeReason, charge.Amount);

            return new ApiResponse<BatchChargeResult>
            {
                Success = true,
                Message = "Charge processed and archived successfully",
                Data = new BatchChargeResult
                {
                    ProcessedCount = 1,
                    ProcessedAmount = charge.Amount,
                    ProcessingDate = now
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing charge");

            charge.Status = BatchChargeStatus.Failed;
            charge.FailureReason = ex.Message;
            charge.UpdatedAt = DateTime.UtcNow;

            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Error processing charge",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    // In repository
    public async Task<ApiResponse<List<BatchChargeEntry>>> GetFailedChargesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.BatchCharges
                .Where(c => c.Status == BatchChargeStatus.Failed);

            // Apply date filters only if provided
            if (startDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt <= endDate.Value);
            }

            var failedCharges = await query.ToListAsync();

            return new ApiResponse<List<BatchChargeEntry>>
            {
                Success = true,
                Message = $"Retrieved {failedCharges.Count} failed charges",
                Data = failedCharges,
                Length = failedCharges.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed charges");
            return new ApiResponse<List<BatchChargeEntry>>
            {
                Success = false,
                Message = "Error retrieving failed charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<BatchChargeResult>> QueueSingleChargeAsync(AccountChargeDetail charge, bool processImmediately = false)
    {
        // Validate amount is multiple of 4 or 10
        if (!(charge.Amount % 4 == 0 || charge.Amount % 10 == 0))
        {
            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Invalid charge amount",
                Errors = new List<string> {
            $"Invalid charge amount: {charge.Amount}. Amount must be a multiple of 4 for SMS alerts or multiple of 10 for QBE alerts."
        }
            };
        }

        // Determine charge reason based on amount
        var ChargeReason = charge.Amount % 10 == 0
            ? "quick balance enquiry alert charge"
            : "sms-alert charge";


        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Starting single charge queue for account {Account}. ProcessImmediately: {Immediate}",
                charge.AccountNumber, processImmediately);

            var now = DateTime.UtcNow;
            var entry = new BatchChargeEntry
            {
                AccountNumber = charge.AccountNumber,
                Amount = charge.Amount,
                ChargeReason = ChargeReason,
                FailureReason = "NONE",
                Status = processImmediately ? BatchChargeStatus.Processing : BatchChargeStatus.Pending,
                RetryCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _context.BatchCharges.AddAsync(entry);
            await _context.SaveChangesAsync();

            var result = new BatchChargeResult
            {
                TotalCharges = 1,
                TotalAmount = entry.Amount,
                ProcessingDate = now
            };

            if (processImmediately)
            {
                var processResult = await ProcessChargeAsync(entry);
                if (!processResult.Success)
                {
                    await transaction.RollbackAsync();
                    return processResult;
                }
                result = processResult.Data;
            }

            await transaction.CommitAsync();

            return new ApiResponse<BatchChargeResult>
            {
                Success = true,
                Message = processImmediately
                    ? "Charge processed successfully"
                    : "Charge queued successfully for processing",
                Data = result
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing single charge");
            return new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "Error processing charge",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<List<BatchChargeArchive>>> GetArchivedChargesAsync(
    DateTime? startDate = null,
    DateTime? endDate = null,
    string accountNumber = null)
    {
        try
        {
            var query = _context.BatchChargeArchives.AsQueryable();

            // Apply filters if provided
            if (startDate.HasValue)
            {
                query = query.Where(c => c.ProcessedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.ProcessedDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                query = query.Where(c => c.AccountNumber == accountNumber);
            }

            var archivedCharges = await query.ToListAsync();

            return new ApiResponse<List<BatchChargeArchive>>
            {
                Success = true,
                Message = $"Retrieved {archivedCharges.Count} archived charges",
                Data = archivedCharges,
                Length = archivedCharges.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archived charges");
            return new ApiResponse<List<BatchChargeArchive>>
            {
                Success = false,
                Message = "Error retrieving archived charges",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
