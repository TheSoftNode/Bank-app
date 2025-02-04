using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.DTOs.BalanceEnquiryDTOs;
using Entities_Dtos.Responses;

namespace Data_service.IRepository;

public interface IQuickBalanceEnquiryRepository : IGenericRepository<QuickBalanceEnquiry>
{
    Task<bool> CreateEnquiryAsync(QuickBalanceEnquiryDto enquiryDto);
    Task<IEnumerable<QuickBalanceEnquiry>> GetUnchargedEnquiriesAsync();
    Task<QuickBalanceEnquiryResponse> ProcessEnquiryChargeAsync(Guid enquiryId);
    Task<decimal> GetTotalQBEChargesAsync(string customerNumber, DateTime startDate, DateTime endDate);
    Task<QuickBalanceEnquiry> GetLatestEnquiryByCustomerAsync(string customerNumber);
    Task<ApiResponse<List<QBECustomerSummary>>> GetCustomersForBillingAsync(DateTime startDate, DateTime endDate);
    Task<ApiResponse<List<QuickBalanceEnquiry>>> GetUnchargedEnquiriesForAccountAsync(Guid accountId);
 }
