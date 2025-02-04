using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs.BalanceEnquiryDTOs;
using Entities_Dtos.DTOs.BatchProcessing;
using Entities_Dtos.DTOs.DirectDebitDTOs;
using Entities_Dtos.Responses;

namespace Data_service.IRepository;

public interface IBatchProcessingRepository
{
    Task<ApiResponse<ProcessingResult>> ProcessDailyChargesAsync();
    Task<bool> ProcessRetryQueueAsync();
    Task<ApiResponse<TelcoSettlementResult>> ProcessTelcoSettlementsAsync();
    
    Task<ApiResponse<ReconciliationResult>> ReconcileFailedTransactionsAsync(DateTime date);
    Task<ApiResponse<MonthEndProcessingResult>> ProcessMonthEndChargesAsync(DateTime monthEndDate);
    Task<ApiResponse<QBEProcessingResult>> ProcessMonthlyQBEChargesAsync(DateTime startDate, DateTime endDate);
}
