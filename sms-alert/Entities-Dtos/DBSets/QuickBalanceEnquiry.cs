using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets
{
    public class QuickBalanceEnquiry : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid CustomerAccountId { get; set; }
        public decimal ChargeAmount { get; set; }  // NGN 10 VAT Inclusive
        public decimal SessionCharge { get; set; }  // NGN 6.98
        public TelcoProvider TelcoProvider { get; set; }
        public bool IsCharged { get; set; }
        public bool IsSettled { get; set; } = false;
        public DateTime? SettlementDate { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public CustomerAccount Account { get; set; }
    }
}
