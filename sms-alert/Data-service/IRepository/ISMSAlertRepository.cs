using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;

namespace Data_service.IRepository;

public interface ISMSAlertRepository : IGenericRepository<SMSAlert>
{
    Task<bool> CreateAlertAsync(CreateSMSAlertDto alertDto);
    Task<SMSAlert> GetLatestAlertByCustomerAsync(string customerNumber);
    Task<SMSAlertResponse> GetAlertStatusAsync(Guid alertId);
    Task<IEnumerable<SMSAlert>> GetUnchargedAlertsAsync();
    Task<IEnumerable<SMSAlert>> GetAlertsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<bool> UpdateDeliveryStatusAsync(Guid alertId, DeliveryStatus status);
    Task<decimal> GetTotalChargesForPeriodAsync(string customerNumber, DateTime startDate, DateTime endDate);
}
