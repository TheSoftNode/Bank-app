using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs.BatchChargeDTOs;
using Entities_Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace sms_alert.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchChargeController : ControllerBase
{
    private readonly IBatchChargeRepository _batchChargeRepository;
    private readonly ILogger<BatchChargeController> _logger;

    public BatchChargeController(
        IBatchChargeRepository batchChargeRepository,
        ILogger<BatchChargeController> logger)
    {
        _batchChargeRepository = batchChargeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Queue or process charges for multiple accounts
    /// </summary>
    [HttpPost("queue")]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BatchChargeResult>>> QueueCharges(
        [FromBody] BatchChargeRequest request)
    {
        try
        {
            if (!request.Charges.Any())
            {
                return BadRequest(new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "No charges provided",
                    Errors = new List<string> { "The charges list cannot be empty" }
                });
            }

            // Validate individual charges
            var validationErrors = ValidateCharges(request.Charges);
            if (validationErrors.Any())
            {
                return BadRequest(new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "Invalid charge details",
                    Errors = validationErrors
                });
            }

            // Queue and optionally process the charges
            var queueResult = await _batchChargeRepository.QueueChargesAsync(
                request.Charges,
                request.ProcessImmediately);

            return queueResult.Success ? Ok(queueResult) : BadRequest(queueResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch charges request");
            return StatusCode(500, new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "An error occurred while processing the charges",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Queue or process a single charge
    /// </summary>
    [HttpPost("queue/single")]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BatchChargeResult>>> QueueSingleCharge(
        [FromBody] AccountChargeDetail charge,
        [FromQuery] bool processImmediately = false)
    {
        try
        {
            if (string.IsNullOrEmpty(charge.AccountNumber))
            {
                return BadRequest(new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "Account number is required",
                    Errors = new List<string> { "Account number cannot be empty" }
                });
            }

            if (charge.Amount <= 0)
            {
                return BadRequest(new ApiResponse<BatchChargeResult>
                {
                    Success = false,
                    Message = "Invalid amount",
                    Errors = new List<string> { "Amount must be greater than zero" }
                });
            }

            var result = await _batchChargeRepository.QueueSingleChargeAsync(charge, processImmediately);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing single charge request");
            return StatusCode(500, new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "An error occurred while processing the charge",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all failed charges within a date range
    /// </summary>
    [HttpGet("failed")]
    [ProducesResponseType(typeof(ApiResponse<List<BatchChargeEntry>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BatchChargeEntry>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<List<BatchChargeEntry>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BatchChargeEntry>>>> GetFailedCharges(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var failedCharges = await _batchChargeRepository.GetFailedChargesAsync(startDate, endDate);
            return Ok(failedCharges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed charges");
            return StatusCode(500, new ApiResponse<List<BatchChargeEntry>>
            {
                Success = false,
                Message = "An error occurred while retrieving failed charges",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get archived charges within a date range
    /// </summary>
    [HttpGet("archived")]
    [ProducesResponseType(typeof(ApiResponse<List<BatchChargeArchive>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BatchChargeArchive>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<List<BatchChargeArchive>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BatchChargeArchive>>>> GetArchivedCharges(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string accountNumber = null)
    {
        try
        {
            var archivedCharges = await _batchChargeRepository.GetArchivedChargesAsync(
                startDate,
                endDate,
                accountNumber);

            return Ok(archivedCharges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archived charges");
            return StatusCode(500, new ApiResponse<List<BatchChargeArchive>>
            {
                Success = false,
                Message = "An error occurred while retrieving archived charges",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Manually process pending charges
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BatchChargeResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BatchChargeResult>>> ProcessPendingCharges()
    {
        try
        {
            var result = await _batchChargeRepository.ProcessPendingChargesAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending charges");
            return StatusCode(500, new ApiResponse<BatchChargeResult>
            {
                Success = false,
                Message = "An error occurred while processing pending charges",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private List<string> ValidateCharges(List<AccountChargeDetail> charges)
    {
        var errors = new List<string>();

        foreach (var charge in charges)
        {
            if (string.IsNullOrWhiteSpace(charge.AccountNumber))
            {
                errors.Add($"Account number is required for charge");
            }

            if (charge.Amount <= 0)
            {
                errors.Add($"Invalid amount {charge.Amount} for charge ");
            }
        }

        return errors;
    }
}
