

namespace Entities_Dtos.Responses;

public class FailedChargeDetail
{
    public string Email { get; set; }
    public string AccountNumber { get; set; }
    public decimal ChargeAmount { get; set; }
    public string FailureReason { get; set; }
    public DateTime FailureDate { get; set; }
}
