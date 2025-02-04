using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class AccountTransactionRepository : GenericRepository<AccountTransaction>, IAccountTransactionRepository
{
    private readonly ISMSAlertRepository _smsAlertRepository;

    public AccountTransactionRepository(
        SMSAlertDbContext context, 
        ILogger<AccountTransactionRepository> logger,
        ISMSAlertRepository smsAlertRepository) : base(context, logger)
    {
        _smsAlertRepository = smsAlertRepository;
    }


    public async Task<ApiResponse<AccountTransaction>> CreateTransactionAsync(CustomerAccount account, decimal amount, TransactionType type, string reference, string originalReference = null)
    {
        try
        {

            try
            {
                // For reversals, find the original transaction
                AccountTransaction? originalTransaction = new();
                if (type == TransactionType.Reversal && !string.IsNullOrEmpty(originalReference))
                {
                    originalTransaction = await dbSet
                        .FirstOrDefaultAsync(t => t.TransactionReference == originalReference);

                    if (originalTransaction == null)
                    {
                        _logger.LogError($"Original transaction with reference {originalReference} not found for reversal");
                        return new ApiResponse<AccountTransaction>
                        {
                            Success = false,
                            Message = "Original transaction not found for reversal",
                            Data = null!,
                            Errors = new List<string> { $"Transaction with reference {originalReference} not found" }
                        };
                    }

                    if (originalTransaction.Amount != amount)
                    {
                        _logger.LogError($"Reversal amount {amount} does not match original transaction amount {originalTransaction.Amount}");
                        return new ApiResponse<AccountTransaction>
                        {
                            Success = false,
                            Message = "Invalid reversal amount",
                            Data = null!,
                            Errors = new List<string> { $"Reversal amount {amount} does not match original transaction amount {originalTransaction.Amount}" }
                        };
                    }
                }

                var transactionEntity = new AccountTransaction
                {
                    CustomerAccountId = account.Id,
                    Amount = amount,
                    TransactionType = type,
                    TransactionReference = reference,
                    OriginalTransactionReference = originalReference,
                    Narration = GetNarrationByType(type, reference, originalReference),
                    ProcessedDate = DateTime.UtcNow
                };

                await dbSet.AddAsync(transactionEntity);

                // Update account balance based on transaction type
                var balanceUpdateResult = await UpdateAccountBalance(account, amount, type, originalTransaction, reference);
                if (!balanceUpdateResult.Success)
                {
                    return new ApiResponse<AccountTransaction>
                    {
                        Success = false,
                        Message = "Failed to update account balance",
                        Data = null!,
                        Errors = balanceUpdateResult.Errors
                    };
                }

                // Create SMS Alert
                var alertDto = new CreateSMSAlertDto
                {
                    Email = account.Customer.Email,
                    AccountNumber = account.AccountNumber,
                    MessageContent = GenerateAlertMessage(account.AccountNumber, amount, type, transactionEntity.Narration, account.Balance),
                    AlertType = "TransactionNotification"
                };

                bool alertCreated = await _smsAlertRepository.CreateAlertAsync(alertDto);
                if (!alertCreated)
                {
                    _logger.LogWarning("Failed to create SMS alert for transaction {Reference}", reference);
                    // We might still want to commit the transaction even if SMS alert fails
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<AccountTransaction>
                {
                    Success = true,
                    Message = "Transaction created successfully",
                    Data = transactionEntity
                };
            }
            catch (Exception ex)
            {
                throw; // Re-throw to be caught by outer try-catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateTransactionAsync method error", typeof(AccountTransactionRepository));
            return new ApiResponse<AccountTransaction>
            {
                Success = false,
                Message = "Error creating transaction",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<ApiResponse<bool>> UpdateAccountBalance(CustomerAccount account, decimal amount, TransactionType type, AccountTransaction originalTransaction, string reference)
    {
        try
        {
            switch (type)
            {
                case TransactionType.Credit:
                    account.Balance += amount;
                    _logger.LogInformation($"Credit transaction: Added {amount:C} to account {account.AccountNumber}. New balance: {account.Balance:C}");
                    break;

                case TransactionType.Debit:
                    if (account.Balance < amount)
                    {
                        _logger.LogWarning($"Insufficient funds for debit. Account: {account.AccountNumber}, Required: {amount:C}, Available: {account.Balance:C}");
                        return new ApiResponse<bool>
                        {
                            Success = false,
                            Message = "Insufficient funds",
                            Data = false,
                            Errors = new List<string> { $"Insufficient funds for debit. Required: {amount:C}, Available: {account.Balance:C}" }
                        };
                    }
                    account.Balance -= amount;
                    _logger.LogInformation($"Debit transaction: Deducted {amount:C} from account {account.AccountNumber}. New balance: {account.Balance:C}");
                    break;

                case TransactionType.Reversal:
                    if (originalTransaction != null)
                    {
                        switch (originalTransaction.TransactionType)
                        {
                            case TransactionType.Credit:
                                if (account.Balance < amount)
                                {
                                    _logger.LogWarning($"Insufficient funds for credit reversal. Account: {account.AccountNumber}, Required: {amount:C}, Available: {account.Balance:C}");
                                    return new ApiResponse<bool>
                                    {
                                        Success = false,
                                        Message = "Insufficient funds for reversal",
                                        Data = false,
                                        Errors = new List<string> { $"Insufficient funds for credit reversal. Required: {amount:C}, Available: {account.Balance:C}" }
                                    };
                                }
                                account.Balance -= amount;
                                break;

                            case TransactionType.Debit:
                                account.Balance += amount;
                                break;

                            default:
                                _logger.LogError($"Cannot reverse a transaction of type {originalTransaction.TransactionType}");
                                return new ApiResponse<bool>
                                {
                                    Success = false,
                                    Message = "Invalid transaction type for reversal",
                                    Data = false,
                                    Errors = new List<string> { $"Cannot reverse a transaction of type {originalTransaction.TransactionType}" }
                                };
                        }

                        originalTransaction.IsReversed = true;
                        originalTransaction.ReversalReference = reference;
                        originalTransaction.ReversalDate = DateTime.UtcNow;
                        _context.Update(originalTransaction);
                    }
                    break;

                default:
                    _logger.LogError($"Unsupported transaction type: {type}");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid transaction type",
                        Data = false,
                        Errors = new List<string> { $"Unsupported transaction type: {type}" }
                    };
            }

            _context.CustomerAccounts.Update(account);
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account balance");
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error updating account balance",
                Errors = new List<string> { ex.Message }
            };
        }

    }

    private string GenerateAlertMessage(string accountNumber, decimal amount, TransactionType type, string narration, decimal newBalance)
    {
        string transactionType = type switch
        {
            TransactionType.Credit => "CR",
            TransactionType.Debit => "DR",
            TransactionType.Reversal => "REV",
            _ => "TXN"
        };

        return $"Your account {MaskAccountNumber(accountNumber)} has been {transactionType} with NGN {amount:N2}. " +
               $"Narration: {narration}. " +
               $"Balance: NGN {newBalance:N2}";
    }

    private string MaskAccountNumber(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length < 4)
            return accountNumber;

        return $"*****{accountNumber[^4..]}";
    }

    public async Task<IEnumerable<AccountTransaction>> GetTransactionsByAccountNumberAsync(string accountNumber)
    {
        try
        {
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null) return new List<AccountTransaction>();

            return await dbSet
                .Where(t => t.CustomerAccountId == account.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetTransactionsByAccountNumberAsync method error", typeof(AccountTransactionRepository));
            return new List<AccountTransaction>();
        }
    }

    public async Task<IEnumerable<AccountTransaction>> GetTransactionsByDateRangeAsync(
        string accountNumber, DateTime startDate, DateTime endDate)
    {
        try
        {
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null) return new List<AccountTransaction>();

            return await dbSet
                .Where(t => t.CustomerAccountId == account.Id &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetTransactionsByDateRangeAsync method error", typeof(AccountTransactionRepository));
            return new List<AccountTransaction>();
        }
    }

    public async Task<bool> ValidateTransactionAsync(string transactionReference)
    {
        try
        {
            return await dbSet.AnyAsync(t => t.TransactionReference == transactionReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ValidateTransactionAsync method error", typeof(AccountTransactionRepository));
            return false;
        }
    }
    public async Task<AccountTransaction> GetByReferenceAsync(string reference)
    {
        try
        {
            return await dbSet
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.TransactionReference == reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetByReferenceAsync method error", typeof(AccountTransactionRepository));
            return null;
        }
    }

    private string GetNarrationByType(TransactionType type, string reference, string originalReference = null)
    {
        return type switch
        {
            TransactionType.Debit => $"DR: SMS/QBE Charge - {reference}",
            TransactionType.Credit => $"CR: Credit - {reference}",
            TransactionType.Reversal => originalReference != null
                ? $"REV: Charge Reversal of {originalReference} - {reference}"
                : $"REV: Charge Reversal - {reference}",
            _ => $"Transaction - {reference}"
        };
    }
}
