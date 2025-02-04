using Entities_Dtos.DBSets;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;

namespace Data_service.IRepository;

public interface IAccountTransactionRepository : IGenericRepository<AccountTransaction>
{
    Task<ApiResponse<AccountTransaction>> CreateTransactionAsync(CustomerAccount account, decimal amount, TransactionType type, string reference, string originalReference = null);
    Task<IEnumerable<AccountTransaction>> GetTransactionsByDateRangeAsync(string accountNumber, DateTime startDate, DateTime endDate);
    Task<bool> ValidateTransactionAsync(string transactionReference);
    Task<IEnumerable<AccountTransaction>> GetTransactionsByAccountNumberAsync(string accountNumber);
    Task<AccountTransaction> GetByReferenceAsync(string reference);
}
