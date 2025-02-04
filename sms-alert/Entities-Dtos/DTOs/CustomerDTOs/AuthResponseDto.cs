using Entities_Dtos.Responses;

namespace Entities_Dtos.DTOs.CustomerDTOs;

public class AuthResponseDto
{
    public string Token { get; set; }
    public CustomerResponseDto Customer { get; set; }
}
