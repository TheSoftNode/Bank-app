using Data_service.IConfiguration;
using Entities_Dtos.DTOs;
using Entities_Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace sms_alert.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuickBalanceEnquiryController : ControllerBase
{
    private readonly ISMSAlertUnitOfWork _unitOfWork;
    private readonly ILogger<QuickBalanceEnquiryController> _logger;

    public QuickBalanceEnquiryController(
        ISMSAlertUnitOfWork unitOfWork,
        ILogger<QuickBalanceEnquiryController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }



    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<QuickBalanceEnquiryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QuickBalanceEnquiryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<QuickBalanceEnquiryResponse>>> CreateEnquiry([FromBody] QuickBalanceEnquiryDto enquiryDto)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {

            var customer = await _unitOfWork.Customers.GetByEmailAsync(enquiryDto.Email);
            if (customer == null)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<QuickBalanceEnquiryResponse>
                {
                    Success = false,
                    Message = "Customer not found",
                    Data = null
                });
            }

            if (customer.PhoneNumber != enquiryDto.PhoneNumber)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<QuickBalanceEnquiryResponse>
                {
                    Success = false,
                    Message = "Phone number does not match customer's registered number",
                    Data = null
                });
            }

            // Create the QBE entry
            var result = await _unitOfWork.QuickBalanceEnquiries.CreateEnquiryAsync(enquiryDto);
            if (!result)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<QuickBalanceEnquiryResponse>
                {
                    Success = false,
                    Message = "Failed to create quick balance enquiry",
                    Data = null
                });
            }

            // Get the created enquiry
            var enquiry = await _unitOfWork.QuickBalanceEnquiries.GetLatestEnquiryByCustomerAsync(enquiryDto.Email);
            if (enquiry == null)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse<QuickBalanceEnquiryResponse>
                {
                    Success = false,
                    Message = "Quick balance enquiry created but unable to retrieve for accounting entry",
                    Data = null
                });
            }

            // Create the accounting entries
            var accountingResult = await _unitOfWork.AccountingEntries.CreateQBEEntryAsync(enquiry);
            if (!accountingResult)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("QBE created but failed to create accounting entry for EnquiryId: {EnquiryId}", enquiry.Id);
                return StatusCode(500, new ApiResponse<QuickBalanceEnquiryResponse>
                {
                    Success = false,
                    Message = "Quick balance enquiry created but failed to create accounting entries",
                    Data = null,
                    Errors = new List<string> { $"Failed to create accounting entries for enquiry {enquiry.Id}" }
                });
            }

            await _unitOfWork.SaveToDbAsync();
            await transaction.CommitAsync();

            var response = new QuickBalanceEnquiryResponse
            {
                EnquiryId = enquiry.Id,
                CustomerNumber = enquiry.Customer?.Email,
                AccountNumber = enquiry.Account?.AccountNumber,
                Balance = enquiry.Account?.Balance ?? 0,
                ChargeAmount = enquiry.ChargeAmount,
                SessionCharge = enquiry.SessionCharge,
                TelcoProvider = enquiry.TelcoProvider.ToString(),
                IsCharged = enquiry.IsCharged,
                CreatedAt = enquiry.CreatedAt
            };

            return Ok(new ApiResponse<QuickBalanceEnquiryResponse>
            {
                Success = true,
                Message = "Quick balance enquiry and accounting entries created successfully",
                Data = response
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error in CreateEnquiry method");
            return StatusCode(500, new ApiResponse<QuickBalanceEnquiryResponse>
            {
                Success = false,
                Message = "An error occurred while creating the quick balance enquiry",
                Errors = new List<string> { ex.Message }
            });
        }
    }

}
