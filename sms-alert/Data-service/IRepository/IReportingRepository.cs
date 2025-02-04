using Entities_Dtos.DTOs;
using Entities_Dtos.Responses;

namespace Data_service.IRepository;

public interface IReportingRepository
{
    Task<AlertChargeReport> GetAlertChargeReportAsync(AlertChargeSearchDto searchDto);
    Task<UnrecoupedChargesReportDto> GetUnrecoupedChargesReportAsync(DateTime asOfDate);
    Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime date);
    Task<IEnumerable<FailedChargeDetail>> GetFailedChargesDetailAsync(DateTime startDate, DateTime endDate);
}
