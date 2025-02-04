namespace Entities_Dtos.DBSets
{
    public class Customer : BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "Customer";

        public bool IsSMSAlertEnabled { get; set; } = true;
        public DateTime LastSmsAlertCheck { get; set; }

        // Bank-specific properties
        public DateTime? LastTransactionDate { get; set; } 
        public string PreferredLanguage { get; set; } = "en";
        public bool IsBlacklisted { get; set; } = false;
        public string? BlacklistReason { get; set; }

        // Navigation properties
        public List<CustomerAccount> Accounts { get; set; } = new();
        public List<SMSAlert> SMSAlerts { get; set; } = new();
        public List<DirectDebitQueue> DebitQueues { get; set; } = new();
    }

}
