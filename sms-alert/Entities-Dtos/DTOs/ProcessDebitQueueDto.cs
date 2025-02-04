namespace Entities_Dtos.DTOs
{
    public class ProcessDebitQueueDto
    {
        public string CustomerNumber { get; set; }
        public string AccountNumber { get; set; }
        public decimal ChargeAmount { get; set; }
        public DateTime ProcessDate { get; set; }
    }
}
