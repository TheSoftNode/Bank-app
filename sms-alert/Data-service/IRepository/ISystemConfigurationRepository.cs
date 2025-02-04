using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.Responses;

public interface ISystemConfigurationRepository : IGenericRepository<SystemConfiguration>
{
    Task<string> GetConfigValueAsync(string key, string defaultValue);
    Task<ApiResponse<Dictionary<string, string>>> GetJobConfigurationsAsync();
    Task<ApiResponse<List<SystemConfiguration>>> GetAllConfigsAsync();
    Task<ApiResponse<SystemConfiguration>> CreateConfigAsync(string key, string value, string description, string createdBy);
    Task<ApiResponse<SystemConfiguration>> UpdateConfigAsync(string key, string value, string modifiedBy);
    Task<ApiResponse<bool>> DeleteConfigAsync(string key, string modifiedBy);
}