using Data_service.IConfiguration;
using Hangfire;
using sms_alert.BackgroundJobs.Interfaces;

namespace sms_alert.BackgroundJobs.services;
public class SMSJobs : ISMSJobs
{
    private readonly ISMSAlertUnitOfWork _unitOfWork;
    private readonly ILogger<SMSJobs> _logger;
    private readonly ISystemConfigurationRepository _configRepository;

    public SMSJobs(
        ISMSAlertUnitOfWork unitOfWork,
        ILogger<SMSJobs> logger,
        ISystemConfigurationRepository configRepository)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configRepository = configRepository;
    }

    public async Task CheckAndConfigureAllJobs()
    {
        try
        {
            _logger.LogInformation("Starting configuration of all SMS jobs");

            // Get all job configurations at once to reduce database calls
            var jobConfigs = await _configRepository.GetJobConfigurationsAsync();
            if (!jobConfigs.Success)
            {
                throw new InvalidOperationException("Failed to retrieve job configurations: " + string.Join(", ", jobConfigs.Errors ?? new List<string>()));
            }

            await ConfigureDailyProcessingJob(jobConfigs.Data);
            await ConfigureRetryJob(jobConfigs.Data);
            await ConfigureMonthlyDebitJob(jobConfigs.Data);
            await ConfigureConsolidationJob();

            _logger.LogInformation("Successfully configured all SMS jobs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring SMS jobs");
            throw;
        }
    }

    public async Task ConfigureDailyProcessingJob(Dictionary<string, string> jobConfigs = null)
    {
        try
        {
            var processingTime = jobConfigs != null && jobConfigs.ContainsKey("JobDailyProcessingTime")
                ? jobConfigs["JobDailyProcessingTime"]
                : await _configRepository.GetConfigValueAsync("JobDailyProcessingTime", "00:01");
            
            Console.WriteLine($"processingTime {processingTime.ToString()}");

            if (!TimeSpan.TryParse(processingTime, out TimeSpan time))
            {
                throw new ArgumentException($"Invalid time format for JobDailyProcessingTime: {processingTime}");
            }

            _logger.LogInformation("Configuring daily processing job for {Time}", processingTime);

            RecurringJob.AddOrUpdate(
                "sms-daily-processing",
                () => _unitOfWork.BatchProcessing.ProcessDailyChargesAsync(),
                Cron.Daily(time.Hours, time.Minutes)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring daily processing job");
            throw;
        }
    }

    public async Task ConfigureRetryJob(Dictionary<string, string> jobConfigs = null)
    {
        try
        {
            string retryAttemptsStr = jobConfigs != null && jobConfigs.ContainsKey("JobDailyRetryAttempts")
                ? jobConfigs["JobDailyRetryAttempts"]
                : await _configRepository.GetConfigValueAsync("JobDailyRetryAttempts", "4");

            if (!int.TryParse(retryAttemptsStr, out int retryAttempts) || retryAttempts <= 0)
            {
                throw new ArgumentException($"Invalid retry attempts value: {retryAttemptsStr}");
            }

            _logger.LogInformation("Configuring {RetryAttempts} daily retry jobs", retryAttempts);

            // Remove any existing retry jobs that are no longer needed
            for (int i = retryAttempts + 1; i <= 24; i++)
            {
                RecurringJob.RemoveIfExists($"sms-retry-{i}");
            }

            for (int i = 1; i <= retryAttempts; i++)
            {
                int hour = (24 / (retryAttempts + 1)) * i;
                _logger.LogInformation("Configuring retry job {JobNumber} to run at hour {Hour}", i, hour);

                RecurringJob.AddOrUpdate(
                    $"sms-retry-{i}",
                    () => _unitOfWork.BatchProcessing.ProcessRetryQueueAsync(),
                    Cron.Daily(hour)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring retry jobs");
            throw;
        }
    }

    public async Task ConfigureMonthlyDebitJob(Dictionary<string, string> jobConfigs = null)
    {
        try
        {
            string debitDateStr = jobConfigs != null && jobConfigs.ContainsKey("JobMonthlyDebitDate")
                ? jobConfigs["JobMonthlyDebitDate"]
                : await _configRepository.GetConfigValueAsync("JobMonthlyDebitDate", "25");

            if (!int.TryParse(debitDateStr, out int debitDate) || debitDate < 1 || debitDate > 28)
            {
                throw new ArgumentException($"Invalid debit date: {debitDateStr}. Must be between 1 and 28.");
            }

            _logger.LogInformation("Configuring monthly debit job for day {DebitDate}", debitDate);

            RecurringJob.AddOrUpdate(
                "sms-monthly-debit",
                () => _unitOfWork.BatchProcessing.ProcessMonthEndChargesAsync(DateTime.UtcNow),
                Cron.Monthly(debitDate)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring monthly debit job");
            throw;
        }
    }

    public async Task ConfigureConsolidationJob()
    {
        try
        {
            _logger.LogInformation("Configuring monthly consolidation job");

            RecurringJob.AddOrUpdate(
                "sms-monthly-consolidation",
                () => _unitOfWork.BatchProcessing.ReconcileFailedTransactionsAsync(DateTime.UtcNow.AddMonths(-1)),
                Cron.Monthly(1)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring consolidation job");
            throw;
        }
    }
}