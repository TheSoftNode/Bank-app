using Entities_Dtos.DBSets;
using Entities_Dtos.Responses;

namespace Data_service.IRepository;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer> GetByEmailAsync(string email);
    Task<bool> AnyCustomersExistAsync();
    Task<CustomerAccount> GetNairaAccountForDomAccountAsync(string domAccountNumber);
    Task<IEnumerable<CustomerAccount>> GetAllAccountsAsync(string customerNumber);
    Task<bool> HasSufficientBalanceAsync(string accountNumber, decimal amount);
    Task<ApiResponse<List<Customer>>> GetAllCustomersAsync();
}
