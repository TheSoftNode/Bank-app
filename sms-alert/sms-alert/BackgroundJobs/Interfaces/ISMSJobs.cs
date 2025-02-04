namespace sms_alert.BackgroundJobs.Interfaces;
public interface ISMSJobs
{
    Task ConfigureDailyProcessingJob(Dictionary<string, string> jobConfigs = null);
    Task ConfigureRetryJob(Dictionary<string, string> jobConfigs = null);
    Task ConfigureMonthlyDebitJob(Dictionary<string, string> jobConfigs = null);
    Task ConfigureConsolidationJob();
    Task CheckAndConfigureAllJobs();
}
