namespace sms_alert.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Entities_Dtos.DBSets;
using Entities_Dtos.DTOs;
using Entities_Dtos.DTOs.CustomerDTOs;
using Entities_Dtos.Responses;
using Data_service.IConfiguration;
using System.Security.Claims;
using sms_alert.Services;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ISMSAlertUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;

    public CustomerController(
        ISMSAlertUnitOfWork unitOfWork,
        ILogger<CustomerController> logger,
        IConfiguration configuration,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _tokenService = tokenService;
    }

    // Self-registration endpoint for customers
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] CustomerRegistrationDto registrationDto)
    {
        try
        {
            var existingCustomer = await _unitOfWork.Customers.GetByEmailAsync(registrationDto.Email);
            if (existingCustomer != null)
            {
                return BadRequest(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Email already registered",
                    Data = null
                });
            }

            // Check if this is the first customer (will be admin)
            var isFirstCustomer = !await _unitOfWork.Customers.AnyCustomersExistAsync();

            var customer = new Customer
            {
                Email = registrationDto.Email,
                FirstName = registrationDto.FirstName,
                LastName = registrationDto.LastName,
                PhoneNumber = registrationDto.PhoneNumber,
                PreferredLanguage = registrationDto.PreferredLanguage ?? "en",
                IsSMSAlertEnabled = true,
                LastSmsAlertCheck = DateTime.UtcNow,
                PasswordHash = BC.HashPassword(registrationDto.Password),
                Role = isFirstCustomer ? "Admin" : "Customer" // First user becomes admin
            };

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveToDbAsync();

            // Generate JWT token
            var token = _tokenService.GenerateJwtToken(customer);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Registration successful",
                Data = new AuthResponseDto
                {
                    Token = token,
                    Customer = new CustomerResponseDto
                    {
                        Id = customer.Id,
                        Email = customer.Email,
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        PhoneNumber = customer.PhoneNumber,
                        PreferredLanguage = customer.PreferredLanguage,
                        IsSMSAlertEnabled = customer.IsSMSAlertEnabled,
                        Role = customer.Role
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer registration");
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // Admin creates customer account
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustomerResponseDto>>> CreateCustomer([FromBody] AdminCreateCustomerDto createCustomerDto)
    {
        try
        {
            var existingCustomer = await _unitOfWork.Customers.GetByEmailAsync(createCustomerDto.Email);
            if (existingCustomer != null)
            {
                return BadRequest(new ApiResponse<CustomerResponseDto>
                {
                    Success = false,
                    Message = "Email already registered",
                    Data = null
                });
            }


            // Generate a random password for the customer
            var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 12);

            var customer = new Customer
            {
                Email = createCustomerDto.Email,
                FirstName = createCustomerDto.FirstName,
                LastName = createCustomerDto.LastName,
                PhoneNumber = createCustomerDto.PhoneNumber,
                PreferredLanguage = createCustomerDto.PreferredLanguage ?? "en",
                IsSMSAlertEnabled = true,
                LastSmsAlertCheck = DateTime.UtcNow,
                PasswordHash = BC.HashPassword(tempPassword),
                Role = "Customer"
            };

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveToDbAsync();

            var response = new AdminCustomerResponseDto
            {
                Id = customer.Id,
                Email = customer.Email,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                PhoneNumber = customer.PhoneNumber,
                PreferredLanguage = customer.PreferredLanguage,
                IsSMSAlertEnabled = customer.IsSMSAlertEnabled,
                Role = customer.Role,
                TempPassword = tempPassword 
            };

            return Ok(new ApiResponse<CustomerResponseDto>
            {
                Success = true,
                Message = "Customer created successfully",
                Data = response,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, new ApiResponse<CustomerResponseDto>
            {
                Success = false,
                Message = "An error occurred while creating the customer",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByEmailAsync(loginDto.Email);
            if (customer == null || !BC.Verify(loginDto.Password, customer.PasswordHash))
            {
                return BadRequest(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Data = null
                });
            }

            var token = _tokenService.GenerateJwtToken(customer);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Login successful",
                Data = new AuthResponseDto
                {
                    Token = token,
                    Customer = new CustomerResponseDto
                    {
                        Id = customer.Id,
                        Email = customer.Email,
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        PhoneNumber = customer.PhoneNumber,
                        PreferredLanguage = customer.PreferredLanguage,
                        IsSMSAlertEnabled = customer.IsSMSAlertEnabled,
                        Role = customer.Role
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    
}