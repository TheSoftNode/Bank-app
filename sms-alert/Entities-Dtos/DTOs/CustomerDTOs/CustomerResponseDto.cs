namespace Entities_Dtos.DTOs.CustomerDTOs;

public class CustomerResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string PreferredLanguage { get; set; }
    public bool IsSMSAlertEnabled { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public string Role { get; set; }
}
