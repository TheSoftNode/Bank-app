using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets
{
    public class AccountTransaction : BaseEntity
    {
        public Guid CustomerAccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionReference { get; set; }
        public TransactionType TransactionType { get; set; }
        public string? OriginalTransactionReference { get; set; }
        public string Narration { get; set; }
        public DateTime ProcessedDate { get; set; }
        public bool IsReversed { get; set; }
        public string? ReversalReference { get; set; }
        public DateTime? ReversalDate { get; set; }

        // Navigation properties
        public CustomerAccount Account { get; set; }
    }
}
