using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.Types;
using Entities_Dtos.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class AccountingEntryRepository : GenericRepository<AccountingEntry>, IAccountingEntryRepository
{
    public AccountingEntryRepository(SMSAlertDbContext context, ILogger<AccountingEntryRepository> logger) : base(context, logger)
    {
    }

    public async Task<bool> CreateSMSAlertEntryAsync(SMSAlert alert)
    {
        try
        {
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.Id == alert.CustomerAccountId);

            if (account == null) return false;

            var entries = new List<AccountingEntry>
                {
                    // Debit customer for SMS charge
                    new AccountingEntry
                    {
                        CustomerId = alert.CustomerId,
                        TransactionReferenceId = alert.Id,
                        TransactionReference = $"SMS_CHARGE_{alert.Id}",
                        DebitAmount = alert.ChargeAmount,
                        CreditAmount = 0,
                        DebitAccountNumber = account.AccountNumber,
                        CreditAccountNumber = AccountingConstants.AccountNumbers.SMS_ALERT_INCOME,
                        VATAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                        VATAmount = 0,
                        Narration = $"SMS Alert Charge for {account.AccountNumber}",
                        EntryType = EntryType.SMSAlertCharge,
                        ProcessedBy = "SYSTEM",
                        ProcessedDate = DateTime.UtcNow
                    },
                    // VAT entry
                    new AccountingEntry
                    {
                        CustomerId = alert.CustomerId,
                        TransactionReferenceId = alert.Id,
                        TransactionReference = $"SMS_VAT_{alert.Id}",
                        DebitAmount = alert.VATAmount,
                        CreditAmount = 0,
                        DebitAccountNumber = account.AccountNumber,
                        CreditAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                        VATAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                        VATAmount = alert.VATAmount,
                        Narration = $"VAT on SMS Alert Charge for {account.AccountNumber}",
                        EntryType = EntryType.VATDebit,
                        ProcessedBy = "SYSTEM",
                        ProcessedDate = DateTime.UtcNow
                    }
                };

            await dbSet.AddRangeAsync(entries);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateSMSAlertEntryAsync method error", typeof(AccountingEntryRepository));
            return false;
        }
    }

    public async Task<bool> CreateQBEEntryAsync(QuickBalanceEnquiry enquiry)
    {
        try
        {
            var account = await _context.CustomerAccounts
                .FirstOrDefaultAsync(a => a.Id == enquiry.CustomerAccountId);

            if (account == null) return false;

            var entries = new List<AccountingEntry>
                {
                    // QBE Charge entry
                    new AccountingEntry
                    {
                        CustomerId = enquiry.CustomerId,
                        TransactionReferenceId = enquiry.Id,
                        TransactionReference = $"QBE_{enquiry.Id}",
                        DebitAmount = enquiry.ChargeAmount,
                        CreditAmount = 0,
                        DebitAccountNumber = account.AccountNumber,
                        CreditAccountNumber = AccountingConstants.AccountNumbers.USSD_INCOME,
                        VATAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                        VATAmount = 0,
                        Narration = $"Quick Balance Enquiry Charge for {account.AccountNumber}",
                        EntryType = EntryType.QuickBalanceEnquiryCharge,
                        ProcessedBy = "SYSTEM",
                        ProcessedDate = DateTime.UtcNow
                    },
                    // Telco session charge
                    new AccountingEntry
                    {
                        CustomerId = enquiry.CustomerId,
                        TransactionReferenceId = enquiry.Id,
                        TransactionReference = $"QBE_TELCO_{enquiry.Id}",
                        DebitAmount = enquiry.SessionCharge,
                        CreditAmount = 0,
                        DebitAccountNumber = account.AccountNumber,
                        CreditAccountNumber = GetTelcoSuspenseAccount(enquiry.TelcoProvider),
                        VATAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                        VATAmount = 0,
                        Narration = $"Telco Session Charge for {account.AccountNumber}",
                        EntryType = EntryType.TelcoSessionCharge,
                        ProcessedBy = "SYSTEM",
                        ProcessedDate = DateTime.UtcNow
                    }
                };

            await dbSet.AddRangeAsync(entries);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateQBEEntryAsync method error", typeof(AccountingEntryRepository));
            return false;
        }
    }

    public async Task<bool> CreateVATEntryAsync(decimal amount, string email, EntryType entryType)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null) return false;

            var nairaAccount = customer.Accounts
                .FirstOrDefault(a => a.CurrencyType == CurrencyType.NGN);

            if (nairaAccount == null) return false;

            var vatEntry = new AccountingEntry
            {
                CustomerId = customer.Id,
                TransactionReference = $"VAT_{Guid.NewGuid()}",
                DebitAmount = amount * AccountingConstants.VAT_RATE,
                CreditAmount = 0,
                DebitAccountNumber = nairaAccount.AccountNumber,
                CreditAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                VATAccountNumber = AccountingConstants.AccountNumbers.VAT_PAYABLE,
                VATAmount = amount * AccountingConstants.VAT_RATE,
                Narration = $"VAT for {entryType} on {nairaAccount.AccountNumber}",
                EntryType = EntryType.VATDebit,
                ProcessedBy = "SYSTEM",
                ProcessedDate = DateTime.UtcNow
            };

            await dbSet.AddAsync(vatEntry);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateVATEntryAsync method error", typeof(AccountingEntryRepository));
            return false;
        }
    }

    public async Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime date)
    {
        try
        {
            var startDate = date.Date;
            var endDate = date.Date.AddDays(1).AddSeconds(-1);

            var entries = await dbSet
                .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
                .ToListAsync();

            var reconciliation = new DailyReconciliationDto
            {
                Date = date,
                TotalSMSCharges = entries
                    .Where(e => e.EntryType == EntryType.SMSAlertCharge)
                    .Sum(e => e.DebitAmount),
                TotalQBECharges = entries
                    .Where(e => e.EntryType == EntryType.QuickBalanceEnquiryCharge)
                    .Sum(e => e.DebitAmount),
                TotalVatCollected = entries
                    .Where(e => e.EntryType == EntryType.VATDebit)
                    .Sum(e => e.VATAmount),
                TotalTelcoCharges = entries
                    .Where(e => e.EntryType == EntryType.TelcoSessionCharge)
                    .Sum(e => e.DebitAmount),
                TelcoProviderCharges = GetTelcoChargesBreakdown(entries)
            };

            return reconciliation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetDailyReconciliationAsync method error", typeof(AccountingEntryRepository));
            return null;
        }
    }

    public async Task<bool> ProcessBulkEntriesAsync(IEnumerable<AccountingEntry> entries)
    {
        try
        {
            await dbSet.AddRangeAsync(entries);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} ProcessBulkEntriesAsync method error", typeof(AccountingEntryRepository));
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
            TelcoProvider.NineMobile =>AccountingConstants.AccountNumbers.TelcoSuspense.NINE_MOBILE,
            _ => throw new ArgumentException("Invalid telco provider")
        };
    }

    private Dictionary<string, decimal> GetTelcoChargesBreakdown(IEnumerable<AccountingEntry> entries)
    {
        var telcoCharges = new Dictionary<string, decimal>
            {
                { "MTN", 0 },
                { "AIRTEL", 0 },
                { "GLO", 0 },
                { "9MOBILE", 0 }
            };

        foreach (var entry in entries.Where(e => e.EntryType == EntryType.TelcoSessionCharge))
        {
            if (entry.CreditAccountNumber == AccountingConstants.AccountNumbers.TelcoSuspense.MTN)
                telcoCharges["MTN"] += entry.DebitAmount;
            else if (entry.CreditAccountNumber == AccountingConstants.AccountNumbers.TelcoSuspense.AIRTEL)
                telcoCharges["AIRTEL"] += entry.DebitAmount;
            else if (entry.CreditAccountNumber == AccountingConstants.AccountNumbers.TelcoSuspense.GLO)
                telcoCharges["GLO"] += entry.DebitAmount;
            else if (entry.CreditAccountNumber == AccountingConstants.AccountNumbers.TelcoSuspense.NINE_MOBILE)
                telcoCharges["9MOBILE"] += entry.DebitAmount;
        }

        return telcoCharges;
    }
}
