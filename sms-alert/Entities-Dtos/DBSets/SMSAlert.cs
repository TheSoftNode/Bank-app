using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets
{
    public class SMSAlert : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid? CustomerAccountId { get; set; }  
        public AlertType AlertType { get; set; }
        public string MessageContent { get; set; }
        public DeliveryStatus DeliveryStatus { get; set; }
        public decimal ChargeAmount { get; set; }  // VAT Exclusive amount
        public decimal VATAmount { get; set; }
        public DateTime DeliveryTimestamp { get; set; }
        public bool IsCharged { get; set; } = false;

        // Navigation properties
        public Customer Customer { get; set; }
        public CustomerAccount Account { get; set; }
        public DirectDebitQueue DebitQueue { get; set; }
    }
}
