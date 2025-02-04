using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets
{
    public class AccountingEntry : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid TransactionReferenceId { get; set; }
        public string TransactionReference { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string DebitAccountNumber { get; set; }
        public string CreditAccountNumber { get; set; }
        public string VATAccountNumber { get; set; }
        public decimal VATAmount { get; set; }
        public string Narration { get; set; }
        public EntryType EntryType { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime? ProcessedDate { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
    }
}
