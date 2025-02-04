using Entities_Dtos.DBSets;

namespace sms_alert.Services;

public interface ITokenService
{
    string GenerateJwtToken(Customer customer);
    bool ValidateToken(string token);
}