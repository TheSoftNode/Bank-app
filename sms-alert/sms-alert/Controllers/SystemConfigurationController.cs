namespace sms_alert.Controllers;

using Microsoft.AspNetCore.Mvc;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs.Config;
using Entities_Dtos.Responses;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/[controller]")]
public class SystemConfigurationController : ControllerBase
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly ILogger<SystemConfigurationController> _logger;

    public SystemConfigurationController(
        ISystemConfigurationRepository repository,
        ILogger<SystemConfigurationController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a specific configuration value by key
    /// </summary>
    [HttpGet("value")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<string>>> GetConfigValue(
        [FromQuery] string key,
        [FromQuery] string defaultValue = "")
    {
        try
        {
            var value = await _repository.GetConfigValueAsync(key, defaultValue);
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Configuration value retrieved successfully",
                Data = value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration value for key: {Key}", key);
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while retrieving the configuration value",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Retrieves all job-related configurations
    /// </summary>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, string>>>> GetJobConfigurations()
    {
        try
        {
            var response = await _repository.GetJobConfigurationsAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job configurations");
            return StatusCode(500, new ApiResponse<Dictionary<string, string>>
            {
                Success = false,
                Message = "An error occurred while retrieving job configurations",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Retrieves all active system configurations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SystemConfiguration>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SystemConfiguration>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SystemConfiguration>>>> GetAllConfigs()
    {
        try
        {
            var response = await _repository.GetAllConfigsAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configurations");
            return StatusCode(500, new ApiResponse<List<SystemConfiguration>>
            {
                Success = false,
                Message = "An error occurred while retrieving configurations",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Creates a new system configuration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SystemConfiguration>>> CreateConfig(
        [FromBody] CreateConfigRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<SystemConfiguration>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var userId = User.Identity?.Name ?? "system"; // Get actual user id from claims
            var response = await _repository.CreateConfigAsync(
                request.Key,
                request.Value,
                request.Description,
                userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(
                nameof(GetConfigValue),
                new { key = request.Key },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration");
            return StatusCode(500, new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "An error occurred while creating the configuration",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Updates an existing system configuration
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SystemConfiguration>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SystemConfiguration>>> UpdateConfig(
        [FromBody] UpdateConfigRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<SystemConfiguration>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var userId = User.Identity?.Name ?? "system";
            var response = await _repository.UpdateConfigAsync(
                request.Key,
                request.Value,
                userId);

            if (!response.Success)
            {
                return response.Message.Contains("not found")
                    ? NotFound(response)
                    : BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(500, new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "An error occurred while updating the configuration",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Deletes (soft delete) a system configuration
    /// </summary>
    [HttpDelete("{key}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteConfig(string key)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var response = await _repository.DeleteConfigAsync(key, userId);

            if (!response.Success)
            {
                return response.Message.Contains("not found")
                    ? NotFound(response)
                    : BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the configuration",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
