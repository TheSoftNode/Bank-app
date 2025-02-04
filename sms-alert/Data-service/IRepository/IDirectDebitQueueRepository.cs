using Entities_Dtos.DBSets;
using Entities_Dtos.Types;
using Entities_Dtos.Responses;

namespace Data_service.IRepository;

public interface IDirectDebitQueueRepository : IGenericRepository<DirectDebitQueue>
{
    Task<ApiResponse<DirectDebitQueue>> QueueDebitRequestAsync(SMSAlert alert);
    Task<IEnumerable<DirectDebitQueue>> GetPendingDebitsAsync();
    Task<IEnumerable<DirectDebitQueue>> GetFailedDebitsAsync();
    Task<bool> UpdateQueueStatusAsync(Guid queueId, QueueStatus status, string failureReason = null!);
    Task<bool> RequeueFailedChargesAsync(DateTime startDate, DateTime endDate);
    Task<int> GetRetryCountAsync(Guid queueId);
}
