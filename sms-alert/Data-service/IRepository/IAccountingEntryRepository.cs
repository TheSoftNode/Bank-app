using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.Types;

namespace Data_service.IRepository;

public interface IAccountingEntryRepository : IGenericRepository<AccountingEntry>
{
    Task<bool> CreateSMSAlertEntryAsync(SMSAlert alert);
    Task<bool> CreateQBEEntryAsync(QuickBalanceEnquiry enquiry);
    Task<bool> CreateVATEntryAsync(decimal amount, string customerNumber, EntryType entryType);
    Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime date);
    Task<bool> ProcessBulkEntriesAsync(IEnumerable<AccountingEntry> entries);
}
