using Data_service.IConfiguration;
using Microsoft.AspNetCore.Mvc;
using Entities_Dtos.DTOs;
using Entities_Dtos.DTOs.DirectDebitDTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.DTOs.BalanceEnquiryDTOs;
using Entities_Dtos.DTOs.BatchProcessing;

namespace sms_alert.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchProcessingController : ControllerBase
{
    private readonly ISMSAlertUnitOfWork _unitOfWork;
    private readonly ILogger<BatchProcessingController> _logger;

    public BatchProcessingController(
        ISMSAlertUnitOfWork unitOfWork,
        ILogger<BatchProcessingController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost("process-daily-charges")]
    [ProducesResponseType(typeof(ApiResponse<ProcessingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProcessingResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProcessingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProcessingResult>>> ProcessDailyCharges()
    {
        try
        {
            var result = await _unitOfWork.BatchProcessing.ProcessDailyChargesAsync();
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<ProcessingResult>
                {
                    Success = false,
                    Message = "Failed to process daily charges",
                    Errors = result.Errors,
                    Data = null
                });
            }

            await _unitOfWork.SaveToDbAsync();

            return Ok(new ApiResponse<ProcessingResult>
            {
                Success = true,
                Message = (result.Data.ProcessedSMSAlerts + result.Data.ProcessedQBERequests) == 0
                ? "No pending SMS alerts or QBE requests found requiring processing at this time"
                : $"Successfully processed {result.Data.ProcessedSMSAlerts} SMS alerts and {result.Data.ProcessedQBERequests} QBE requests",
                Data = result.Data,
                Length = result.Data.ProcessedSMSAlerts + result.Data.ProcessedQBERequests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing daily charges");
            return StatusCode(500, new ApiResponse<ProcessingResult>
            {
                Success = false,
                Message = "An error occurred while processing daily charges",
                Errors = new List<string> { ex.Message },
                Data = null
            });
        }
    }

    [HttpPost("process-monthly-qbe")]
    [ProducesResponseType(typeof(ApiResponse<QBEProcessingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QBEProcessingResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QBEProcessingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<QBEProcessingResult>>> ProcessMonthlyQBECharges([FromBody] MonthlyQBEProcessingDto dto)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _unitOfWork.BatchProcessing.ProcessMonthlyQBEChargesAsync(
                dto.StartDate,
                dto.EndDate);

            if (!result.Success)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<QBEProcessingResult>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors,
                    Data = null
                });
            }

            await _unitOfWork.SaveToDbAsync();
            await transaction.CommitAsync();

            return Ok(new ApiResponse<QBEProcessingResult>
            {
                Success = true,
                Message = result.Data.TotalAccounts == 0
                ? $"No accounts found requiring QBE charges processing for the period {dto.StartDate:dd MMM yyyy} to {dto.EndDate:dd MMM yyyy}"
                : $"Successfully processed {result.Data.ProcessedCount} accounts with total charges of NGN {result.Data.TotalChargesAmount:N2}. {result.Data.QueuedForRetryCount} accounts queued for retry.",
                Data = result.Data,
                Length = result.Data.TotalAccounts
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing monthly QBE charges");
            return StatusCode(500, new ApiResponse<QBEProcessingResult>
            {
                Success = false,
                Message = "An error occurred while processing monthly QBE charges",
                Errors = new List<string> { ex.Message },
                Data = null
            });
        }
    }

    [HttpPost("process-retry-queue")]
    public async Task<IActionResult> ProcessRetryQueue()
    {
        try
        {
            var result = await _unitOfWork.BatchProcessing.ProcessRetryQueueAsync();
            if (!result)
                return BadRequest(new { message = "Failed to process retry queue" });

            await _unitOfWork.SaveToDbAsync();
            return Ok(new { message = "Retry queue processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retry queue");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("process-telco-settlements")]
    [ProducesResponseType(typeof(ApiResponse<TelcoSettlementResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TelcoSettlementResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TelcoSettlementResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TelcoSettlementResult>>> ProcessTelcoSettlements()
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _unitOfWork.BatchProcessing.ProcessTelcoSettlementsAsync();
            if (!result.Success)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<TelcoSettlementResult>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors,
                    Data = null
                });
            }

            await _unitOfWork.SaveToDbAsync();
            await transaction.CommitAsync();

            var message = result.Data.ProvidersProcessed == 0
                ? "No pending telco settlements found requiring processing at this time"
                : $"Successfully processed settlements for {result.Data.ProvidersProcessed} telco providers with total amount NGN {result.Data.TotalSettlementAmount:N2}";

            return Ok(new ApiResponse<TelcoSettlementResult>
            {
                Success = true,
                Message = message,
                Data = result.Data,
                Length = result.Data.ProvidersProcessed
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing telco settlements");
            return StatusCode(500, new ApiResponse<TelcoSettlementResult>
            {
                Success = false,
                Message = "An error occurred while processing telco settlements",
                Errors = new List<string> { ex.Message },
                Data = null
            });
        }
    }

    [HttpPost("reconcile-failed-transactions")]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReconciliationResult>>> ReconcileFailedTransactions([FromBody] ReconcileFailedTransactionsDto dto)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _unitOfWork.BatchProcessing.ReconcileFailedTransactionsAsync(dto.Date);
            if (!result.Success)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<ReconciliationResult>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors,
                    Data = null
                });
            }

            await _unitOfWork.SaveToDbAsync();
            await transaction.CommitAsync();

            var message = result.Data.TransactionsProcessed == 0
                ? $"No failed transactions found requiring reconciliation for {dto.Date:dd MMM yyyy}"
                : $"Successfully reconciled {result.Data.TransactionsProcessed} transactions and consolidated NGN {result.Data.ConsolidatedAmount:N2} for {result.Data.ConsolidatedAccounts} accounts";

            return Ok(new ApiResponse<ReconciliationResult>
            {
                Success = true,
                Message = message,
                Data = result.Data,
                Length = result.Data.TransactionsProcessed
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error reconciling failed transactions");
            return StatusCode(500, new ApiResponse<ReconciliationResult>
            {
                Success = false,
                Message = "An error occurred while reconciling failed transactions",
                Errors = new List<string> { ex.Message },
                Data = null
            });
        }
    }

    [HttpPost("process-month-end")]
    [ProducesResponseType(typeof(ApiResponse<MonthEndProcessingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MonthEndProcessingResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MonthEndProcessingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MonthEndProcessingResult>>> ProcessMonthEndCharges([FromBody] MonthEndProcessingDto dto)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _unitOfWork.BatchProcessing.ProcessMonthEndChargesAsync(dto.MonthEndDate);
            if (!result.Success)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<MonthEndProcessingResult>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors,
                    Data = null
                });
            }

            await _unitOfWork.SaveToDbAsync();
            await transaction.CommitAsync();

            var message = result.Data.AccountsProcessed == 0
                ? $"No failed charges found requiring consolidation for {dto.MonthEndDate:MMMM yyyy}"
                : $"Successfully consolidated {result.Data.TransactionsConsolidated} failed charges totaling NGN {result.Data.TotalConsolidatedAmount:N2} for {result.Data.AccountsProcessed} accounts";

            return Ok(new ApiResponse<MonthEndProcessingResult>
            {
                Success = true,
                Message = message,
                Data = result.Data,
                Length = result.Data.AccountsProcessed
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing month-end charges");
            return StatusCode(500, new ApiResponse<MonthEndProcessingResult>
            {
                Success = false,
                Message = "An error occurred while processing month-end charges",
                Errors = new List<string> { ex.Message },
                Data = null
            });
        }
    }
}

