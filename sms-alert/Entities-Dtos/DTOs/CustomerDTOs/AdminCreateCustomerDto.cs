namespace Entities_Dtos.DTOs.CustomerDTOs;

public class AdminCreateCustomerDto
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
}
