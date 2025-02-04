namespace sms_alert.Controllers;

using Microsoft.AspNetCore.Mvc;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Data_service.IConfiguration;
using Entities_Dtos.Responses;

[ApiController]
[Route("api/[controller]")]
public class CustomerAccountController : ControllerBase
{
    private readonly ISMSAlertUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerAccountController> _logger;

    public CustomerAccountController(
        ISMSAlertUnitOfWork unitOfWork,
        ILogger<CustomerAccountController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerAccountResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CustomerAccountResponse>>> CreateAccount([FromBody] CreateCustomerAccountDto dto)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByEmailAsync(dto.Email);
            if (customer == null)
                return NotFound(new ApiResponse<CustomerAccountResponse>
                {
                    Success = false,
                    Message = "Customer not found",
                    Data = null
                });

            // Check for duplicate account number
            var existingAccount = await _unitOfWork.CustomerAccounts.GetByAccountNumberAsync(dto.AccountNumber);
            if (existingAccount != null)
                return BadRequest(new ApiResponse<CustomerAccountResponse>
                {
                    Success = false,
                    Message = "Account number already exists",
                    Data = null
                });

            // Check if customer already has an account of this type
            var existingAccountType = await _unitOfWork.CustomerAccounts
                .GetByCustomerAndTypeAsync(customer.Id, dto.AccountType);
            if (existingAccountType != null)
                return BadRequest(new ApiResponse<CustomerAccountResponse>
                {
                    Success = false,
                    Message = $"Customer already has a {dto.AccountType} account",
                    Data = null
                });

            if (dto.IsDomiciliaryAccount)
            {
                if (string.IsNullOrEmpty(dto.LinkedNigerianAccountNumber))
                    return BadRequest(new ApiResponse<CustomerAccountResponse>
                    {
                        Success = false,
                        Message = "Domiciliary account must be linked to a Nigerian account",
                        Data = null
                    });

                var isValidNigerianAccount = await _unitOfWork.CustomerAccounts
                    .ValidateNigerianAccountForLinkingAsync(dto.LinkedNigerianAccountNumber);

                if (!isValidNigerianAccount)
                    return BadRequest(new ApiResponse<CustomerAccountResponse>
                    {
                        Success = false,
                        Message = "Invalid Nigerian account for linking",
                        Data = null
                    });
            }

            var newAccount = new CustomerAccount
            {
                AccountNumber = dto.AccountNumber,
                CustomerId = customer.Id,
                AccountType = dto.AccountType,
                CurrencyType = dto.CurrencyType,
                BranchSolId = dto.BranchSolId,
                Balance = dto.InitialBalance,
                IsDomiciliaryAccount = dto.IsDomiciliaryAccount,
                LinkedNigerianAccountNumber = dto.LinkedNigerianAccountNumber
            };

            await _unitOfWork.CustomerAccounts.AddAsync(newAccount);
            await _unitOfWork.SaveToDbAsync();

            var response = new CustomerAccountResponse
            {
                AccountId = newAccount.Id,
                AccountNumber = newAccount.AccountNumber,
                Email = customer.Email,
                AccountType = newAccount.AccountType.ToString(),
                CurrencyType = newAccount.CurrencyType.ToString(),
                Balance = newAccount.Balance,
                IsDomiciliaryAccount = newAccount.IsDomiciliaryAccount,
                LinkedNigerianAccountNumber = newAccount.LinkedNigerianAccountNumber
            };

            return Ok(new ApiResponse<CustomerAccountResponse>
            {
                Success = true,
                Message = "Account created successfully",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer account");
            return StatusCode(500, new ApiResponse<CustomerAccountResponse>
            {
                Success = false,
                Message = "An error occurred while creating the account",
                Errors = new List<string> { ex.Message }
            });
        }
    }

}