using Data_service.IConfiguration;
using Data_service.IRepository;
using Data_service.Repository;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Data_service.Data;
public class SMSAlertUnitOfWork : ISMSAlertUnitOfWork, IDisposable
{
    private readonly SMSAlertDbContext _context;
    private readonly ILogger<SMSAlertUnitOfWork> _logger;

    public ICustomerRepository Customers { get; private set; }
    public ISMSAlertRepository SMSAlerts { get; private set; }
    public IBatchChargeRepository BatchCharge { get; private set; }
    public IDirectDebitQueueRepository DirectDebitQueue { get; private set; }
    public IQuickBalanceEnquiryRepository QuickBalanceEnquiries { get; private set; }
    public IAccountingEntryRepository AccountingEntries { get; private set; }
    public IAccountTransactionRepository AccountTransactions { get; private set; }
    public IReportingRepository Reports { get; private set; }
    public IBatchProcessingRepository BatchProcessing { get; private set; }
    public ISystemConfigurationRepository SystemConfigurations { get; private set; }
    public ICustomerAccountRepository CustomerAccounts { get; private set; }

    public SMSAlertUnitOfWork(
        SMSAlertDbContext context,
        ILogger<SMSAlertUnitOfWork> logger,
        ILoggerFactory loggerFactory)
    {
        _context = context;
        _logger = logger;

        // Initialize repositories with typed loggers
        Customers = new CustomerRepository(_context, loggerFactory.CreateLogger<CustomerRepository>());
        AccountingEntries = new AccountingEntryRepository(_context, loggerFactory.CreateLogger<AccountingEntryRepository>());
        SystemConfigurations = new SystemConfigurationRepository(_context, loggerFactory.CreateLogger<SystemConfigurationRepository>());
        DirectDebitQueue = new DirectDebitQueueRepository(_context, loggerFactory.CreateLogger<DirectDebitQueueRepository>());
        CustomerAccounts = new CustomerAccountRepository(_context, loggerFactory.CreateLogger<CustomerAccountRepository>());

        // Repositories that depend on other repositories
        SMSAlerts = new SMSAlertRepository(
            _context,
            loggerFactory.CreateLogger<SMSAlertRepository>(),
            DirectDebitQueue);

        AccountTransactions = new AccountTransactionRepository(_context, loggerFactory.CreateLogger<AccountTransactionRepository>(), SMSAlerts);

        QuickBalanceEnquiries = new QuickBalanceEnquiryRepository(
            _context,
            loggerFactory.CreateLogger<QuickBalanceEnquiryRepository>(),
            DirectDebitQueue);

        Reports = new ReportingRepository(
            _context,
            loggerFactory.CreateLogger<ReportingRepository>());

        // BatchProcessing depends on multiple repositories
        BatchProcessing = new BatchProcessingRepository(
            _context,
            loggerFactory.CreateLogger<BatchProcessingRepository>(),
            DirectDebitQueue,
            AccountingEntries,
            SystemConfigurations
        );

        BatchCharge = new BatchChargeRepository(
            _context, 
            loggerFactory.CreateLogger<BatchChargeRepository>(),
            SystemConfigurations,
            AccountingEntries);
    }

    public async Task SaveToDbAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving to database");
            throw; // Re-throw the exception after logging
        }
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            _context.Dispose();
        }
        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}


//using Data_service.IConfiguration;
//using Data_service.IRepository;
//using Data_service.Repository;
//using Microsoft.Extensions.Logging;

//namespace Data_service.Data;

//public class SMSAlertUnitOfWork : ISMSAlertUnitOfWork, IDisposable
//{
//    private readonly SMSAlertDbContext _context;
//    private readonly ILogger<SMSAlertUnitOfWork> _logger;

//    public IUsersRepository Users { get; private set; }
//    public ICustomerUserRepository CustomerUsers { get; private set; }
//    public IAdminUserRepository AdminUsers { get; private set; }
//    public ICustomerRepository Customers { get; private set; }
//    public ISMSAlertRepository SMSAlerts { get; private set; }
//    public IDirectDebitQueueRepository DirectDebitQueue { get; private set; }
//    public IQuickBalanceEnquiryRepository QuickBalanceEnquiries { get; private set; }
//    public IAccountingEntryRepository AccountingEntries { get; private set; }
//    public IAccountTransactionRepository AccountTransactions { get; private set; }
//    public IReportingRepository Reports { get; private set; }
//    public IBatchProcessingRepository BatchProcessing { get; private set; }
//    public ISystemConfigurationRepository SystemConfigurations { get; private set; }

//    public SMSAlertUnitOfWork(
//        SMSAlertDbContext context,
//        ILoggerFactory loggerFactory)
//    {
//        _context = context;
//        _logger = loggerFactory.CreateLogger("SMSAlertUnitOfWork");

//        // Initialize repositories
//        Users = new UsersRepository(_context, _logger);
//        CustomerUsers = new CustomerUserRepository(_context, _logger);
//        AdminUsers = new AdminUserRepository(_context, _logger);
//        Customers = new CustomerRepository(_context, _logger);
//        AccountTransactions = new AccountTransactionRepository(_context, _logger);
//        AccountingEntries = new AccountingEntryRepository(_context, _logger);
//        SystemConfigurations = new SystemConfigurationRepository(_context, _logger);
//        DirectDebitQueue = new DirectDebitQueueRepository(_context, _logger);

//        // Repositories that depend on other repositories
//        SMSAlerts = new SMSAlertRepository(_context, _logger, DirectDebitQueue);
//        QuickBalanceEnquiries = new QuickBalanceEnquiryRepository(_context, _logger, DirectDebitQueue);
//        Reports = new ReportingRepository(_context, _logger);

//        // BatchProcessing depends on multiple repositories
//        BatchProcessing = new BatchProcessingRepository(
//            _context,
//            _logger,
//            DirectDebitQueue,
//            AccountingEntries,
//            SystemConfigurations
//        );
//    }

//    public async Task SaveToDbAsync()
//    {
//        try
//        {
//            await _context.SaveChangesAsync();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error occurred while saving to database");
//            throw; // Re-throw the exception after logging
//        }
//    }

//    private bool disposed = false;

//    protected virtual void Dispose(bool disposing)
//    {
//        if (!disposed && disposing)
//        {
//            _context.Dispose();
//        }
//        disposed = true;
//    }

//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }
//}
