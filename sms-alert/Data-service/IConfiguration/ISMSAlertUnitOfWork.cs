using Data_service.IRepository;
using Microsoft.EntityFrameworkCore.Storage;

namespace Data_service.IConfiguration;

public interface ISMSAlertUnitOfWork : IDisposable
{
    ICustomerRepository Customers { get; }
    ICustomerAccountRepository CustomerAccounts { get; }
    ISMSAlertRepository SMSAlerts { get; }
    IDirectDebitQueueRepository DirectDebitQueue { get; }
    IQuickBalanceEnquiryRepository QuickBalanceEnquiries { get; }
    IAccountingEntryRepository AccountingEntries { get; }
    IAccountTransactionRepository AccountTransactions { get; }
    IReportingRepository Reports { get; }
    IBatchProcessingRepository BatchProcessing { get; }
    ISystemConfigurationRepository SystemConfigurations { get; }
    Task<IDbContextTransaction> BeginTransactionAsync();

    Task SaveToDbAsync();
}


