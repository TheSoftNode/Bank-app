using Data_service.Data;
using Entities_Dtos.DBSets;
using Entities_Dtos.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data_service.Repository;

public class SystemConfigurationRepository : GenericRepository<SystemConfiguration>, ISystemConfigurationRepository
{
    // Default job configuration values
    private readonly Dictionary<string, string> _defaultJobConfigs = new()
    {
        { "JobDailyProcessingTime", "1" },
        { "JobDailyRetryAttempts", "4" },
        { "JobMonthlyDebitDate", "25" },
        { "JobRetryIntervalHours", "6" },
        { "JobMaxRetryAttempts", "30" }
    };

    public SystemConfigurationRepository(
        SMSAlertDbContext context,
        ILogger<SystemConfigurationRepository> logger) : base(context, logger)
    {
    }

    public async Task<string> GetConfigValueAsync(string key, string defaultValue)
    {
        try
        {
            // First check database for any configuration
            var config = await dbSet
                .FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive);

            if (config != null)
            {
                return config.ConfigValue;
            }

            // If not found in database, check default job configurations
            if (key.StartsWith("Job") && _defaultJobConfigs.ContainsKey(key))
            {
                return _defaultJobConfigs[key];
            }

            // If not found anywhere, return the provided default value
            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetConfigValueAsync method error for key: {Key}", typeof(SystemConfigurationRepository), key);
            return defaultValue;
        }
    }

    public async Task<ApiResponse<Dictionary<string, string>>> GetJobConfigurationsAsync()
    {
        try
        {
            // Start with defaults
            var jobConfigs = new Dictionary<string, string>(_defaultJobConfigs);

            // Get all active job configurations from database
            var dbJobConfigs = await dbSet
                .Where(c => c.IsActive && c.ConfigKey.StartsWith("Job"))
                .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue);

            // Override defaults with any database values
            foreach (var config in dbJobConfigs)
            {
                jobConfigs[config.Key] = config.Value;
            }

            // Add any additional job configurations from database that weren't in defaults
            foreach (var config in dbJobConfigs.Where(c => !jobConfigs.ContainsKey(c.Key)))
            {
                jobConfigs.Add(config.Key, config.Value);
            }

            return new ApiResponse<Dictionary<string, string>>
            {
                Success = true,
                Message = "Job configurations retrieved successfully",
                Data = jobConfigs,
                Length = jobConfigs.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetJobConfigurationsAsync method error", typeof(SystemConfigurationRepository));
            return new ApiResponse<Dictionary<string, string>>
            {
                Success = false,
                Message = "Error retrieving job configurations",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<List<SystemConfiguration>>> GetAllConfigsAsync()
    {
        try
        {
            var configs = await dbSet
                .Where(c => c.IsActive)
                .ToListAsync();

            // Add default job configs that aren't overridden in the database
            var defaultConfigs = _defaultJobConfigs
                .Where(d => !configs.Any(c => c.ConfigKey == d.Key))
                .Select(d => new SystemConfiguration
                {
                    ConfigKey = d.Key,
                    ConfigValue = d.Value,
                    Description = "Default job configuration",
                    IsActive = true,
                    LastModifiedBy = "system",
                    LastModifiedDate = DateTime.UtcNow
                });

            configs.AddRange(defaultConfigs);

            return new ApiResponse<List<SystemConfiguration>>
            {
                Success = true,
                Message = configs.Any() ? "Configurations retrieved successfully" : "No active configurations found",
                Data = configs,
                Length = configs.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAllConfigsAsync method error", typeof(SystemConfigurationRepository));
            return new ApiResponse<List<SystemConfiguration>>
            {
                Success = false,
                Message = "Error retrieving configurations",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<SystemConfiguration>> CreateConfigAsync(string key, string value, string description, string createdBy)
    {
        try
        {
            if (key == "JobDailyProcessingTime")
            {
                if (!TimeSpan.TryParse(value, out TimeSpan time) ||
                    time.Days > 0 || time.Seconds > 0 || time.Milliseconds > 0)
                {
                    return new ApiResponse<SystemConfiguration>
                    {
                        Success = false,
                        Message = "Invalid time format",
                        Errors = new List<string> { "Time must be in HH:mm format (e.g., '14:30' for 2:30 PM)" }
                    };
                }
            }

            // Check if config already exists
            var existingConfig = await dbSet
                .FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive);

            if (existingConfig != null)
            {
                return new ApiResponse<SystemConfiguration>
                {
                    Success = false,
                    Message = "Configuration already exists",
                    Errors = new List<string> { $"Configuration with key {key} already exists" }
                };
            }

            var config = new SystemConfiguration
            {
                ConfigKey = key,
                ConfigValue = value,
                Description = description,
                LastModifiedBy = createdBy,
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            };

            await dbSet.AddAsync(config);
            await _context.SaveChangesAsync();

            return new ApiResponse<SystemConfiguration>
            {
                Success = true,
                Message = "Configuration created successfully",
                Data = config
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} CreateConfigAsync method error", typeof(SystemConfigurationRepository));
            return new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "Error creating configuration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<SystemConfiguration>> UpdateConfigAsync(string key, string value, string modifiedBy)
    {
        try
        {
            if (key == "JobDailyProcessingTime")
            {
                if (!TimeSpan.TryParse(value, out TimeSpan time) ||
                    time.Days > 0 || time.Seconds > 0 || time.Milliseconds > 0)
                {
                    return new ApiResponse<SystemConfiguration>
                    {
                        Success = false,
                        Message = "Invalid time format",
                        Errors = new List<string> { "Time must be in HH:mm format (e.g., '14:30' for 2:30 PM)" }
                    };
                }
            }

            var config = await dbSet
                .FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive);

            if (config == null)
            {
                return new ApiResponse<SystemConfiguration>
                {
                    Success = false,
                    Message = "Configuration not found",
                    Errors = new List<string> { $"Configuration with key {key} not found" }
                };
            }

            config.ConfigValue = value;
            config.LastModifiedBy = modifiedBy;
            config.LastModifiedDate = DateTime.UtcNow;

            _context.Update(config);
            await _context.SaveChangesAsync();

            return new ApiResponse<SystemConfiguration>
            {
                Success = true,
                Message = "Configuration updated successfully",
                Data = config
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateConfigAsync method error", typeof(SystemConfigurationRepository));
            return new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "Error updating configuration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteConfigAsync(string key, string modifiedBy)
    {
        try
        {
            var config = await dbSet
                .FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive);

            if (config == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Configuration not found",
                    Errors = new List<string> { $"Configuration with key {key} not found" }
                };
            }

            config.IsActive = false;
            config.LastModifiedBy = modifiedBy;
            config.LastModifiedDate = DateTime.UtcNow;

            _context.Update(config);
            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Configuration deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} DeleteConfigAsync method error", typeof(SystemConfigurationRepository));
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Error deleting configuration",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}