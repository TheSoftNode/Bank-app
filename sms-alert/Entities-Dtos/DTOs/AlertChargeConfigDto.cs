

namespace Entities_Dtos.DTOs;

public class AlertChargeConfigDto
{
    public TimeSpan ProcessingStartTime { get; set; }
    public int DailyProcessingFrequency { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryIntervalMinutes { get; set; }
    public decimal SMSAlertCharge { get; set; }
    public decimal QBECharge { get; set; }
    public decimal VatRate { get; set; }
}
