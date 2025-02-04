using Data_service.IConfiguration;
using Entities_Dtos.DTOs.AccountTransactionDTOs;
using Entities_Dtos.Responses;
using Entities_Dtos.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace sms_alert.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountTransactionController : ControllerBase
{
    private readonly ISMSAlertUnitOfWork _unitOfWork;
    private readonly ILogger<AccountTransactionController> _logger;

    public AccountTransactionController(
        ISMSAlertUnitOfWork unitOfWork,
        ILogger<AccountTransactionController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    

    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<TransactionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TransactionResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TransactionResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransactionResponseDto>>> CreateTransaction([FromBody] CreateTransactionDto dto)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var account = await _unitOfWork.CustomerAccounts.GetByAccountNumberAsync(dto.AccountNumber);
            if (account == null)
            {
                return NotFound(new ApiResponse<TransactionResponseDto>
                {
                    Success = false,
                    Message = "Account not found",
                    Data = null
                });
            }

            try
            {
                var result = await _unitOfWork.AccountTransactions.CreateTransactionAsync(
                    account,
                    dto.Amount,
                    dto.TransactionType,
                    dto.TransactionReference,
                    dto.OriginalTransactionReference);

                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new ApiResponse<TransactionResponseDto>
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors,
                        Data = null
                    });
                }

                // Get the created transaction for response
                //var createdTransaction = await _unitOfWork.AccountTransactions.GetByReferenceAsync(dto.TransactionReference);
                var createdTransaction = result.Data;

                await _unitOfWork.SaveToDbAsync();
                await transaction.CommitAsync();

                var response = new TransactionResponseDto
                {
                    TransactionId = createdTransaction.Id,
                    AccountNumber = account.AccountNumber,
                    Amount = createdTransaction.Amount,
                    TransactionType = createdTransaction.TransactionType,
                    TransactionReference = createdTransaction.TransactionReference,
                    OriginalTransactionReference = createdTransaction.OriginalTransactionReference,
                    Narration = createdTransaction.Narration,
                    ProcessedDate = createdTransaction.ProcessedDate,
                    NewBalance = account.Balance
                };

                return Ok(new ApiResponse<TransactionResponseDto>
                {
                    Success = true,
                    Message = GetSuccessMessage(dto.TransactionType),
                    Data = response
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, new ApiResponse<TransactionResponseDto>
            {
                Success = false,
                Message = "An error occurred while processing the transaction",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private string GetSuccessMessage(TransactionType type)
    {
        return type switch
        {
            TransactionType.Debit => "Debit transaction processed successfully",
            TransactionType.Credit => "Credit transaction processed successfully",
            TransactionType.Reversal => "Reversal processed successfully",
            _ => "Transaction processed successfully"
        };
    }


    [HttpGet("account/{accountNumber}/all")]
    [ProducesResponseType(typeof(ApiResponse<AccountTransactionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AccountTransactionsDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AccountTransactionsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AccountTransactionsDto>>> GetAllTransactions(string accountNumber)
    {
        try
        {
            // First verify if account exists
            var account = await _unitOfWork.CustomerAccounts.GetByAccountNumberAsync(accountNumber);
            if (account == null)
            {
                return NotFound(new ApiResponse<AccountTransactionsDto>
                {
                    Success = false,
                    Message = "Account not found",
                    Data = null
                });
            }

            var transactions = await _unitOfWork.AccountTransactions
                .GetTransactionsByAccountNumberAsync(accountNumber);

            var response = new AccountTransactionsDto
            {
                AccountNumber = accountNumber,
                TransactionCount = transactions.Count(),
                Transactions = transactions.Select(t => new AccountTransactionResponseDto
                {
                    TransactionReference = t.TransactionReference,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType.ToString(),
                    Narration = t.Narration,
                    CreatedAt = t.CreatedAt,
                    OriginalTransactionReference = t.OriginalTransactionReference,
                    IsReversed = t.IsReversed,
                    ReversalReference = t.ReversalReference
                }).ToList()
            };

            return Ok(new ApiResponse<AccountTransactionsDto>
            {
                Success = true,
                Message = response.TransactionCount == 0
                    ? "No transactions found for this account"
                    : "Transactions retrieved successfully",
                Length = response.TransactionCount,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for account {AccountNumber}", accountNumber);
            return StatusCode(500, new ApiResponse<AccountTransactionsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving transactions",
                Errors = new List<string> { ex.Message }
            });
        }
    }

}


