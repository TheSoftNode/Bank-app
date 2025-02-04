using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Entities_Dtos.Types;

namespace Entities_Dtos.DBSets
{
    public class DirectDebitQueue : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid SMSAlertId { get; set; }
        public Guid SourceAccountId { get; set; }  // Naira account to be debited
        public decimal TotalChargeAmount { get; set; }  // Including VAT
        public QueueStatus Status { get; set; }
        public string TransactionReference { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastRetryDate { get; set; }
        public string FailureReason { get; set; }
        public DateTime? ProcessedDate { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public SMSAlert SMSAlert { get; set; }
        public CustomerAccount SourceAccount { get; set; }
    }
}
