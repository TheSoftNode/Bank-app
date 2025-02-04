namespace Entities_Dtos.Responses;

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string CustomerNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsSMSAlertEnabled { get; set; }
    public string BVN { get; set; }
    public string PreferredLanguage { get; set; }
    public bool IsBlacklisted { get; set; }
    public string BlacklistReason { get; set; }
    public DateTime LastTransactionDate { get; set; }
}
