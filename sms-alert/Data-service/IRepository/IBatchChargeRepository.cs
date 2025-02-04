using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs.BatchChargeDTOs;
using Entities_Dtos.Responses;

namespace Data_service.IRepository;

public interface IBatchChargeRepository
{
    Task<ApiResponse<BatchChargeResult>> QueueChargesAsync(List<AccountChargeDetail> charges, bool processImmediately = false);
    Task<ApiResponse<BatchChargeResult>> ProcessPendingChargesAsync();
    Task<ApiResponse<BatchChargeResult>> QueueSingleChargeAsync(AccountChargeDetail charge, bool processImmediately = false);
    Task<ApiResponse<List<BatchChargeArchive>>> GetArchivedChargesAsync(
    DateTime? startDate = null,
    DateTime? endDate = null,
    string accountNumber = null);
    Task<ApiResponse<BatchChargeResult>> ProcessChargeAsync(BatchChargeEntry charge);
    Task<ApiResponse<List<BatchChargeEntry>>> GetFailedChargesAsync(DateTime? startDate = null, DateTime? endDate = null);
}
