
namespace Entities_Dtos.DTOs
{
    public class UnrecoupedDetailDto
    {
        public string Email { get; set; }
        public string AccountNumber { get; set; }
        public decimal UnrecoupedAmount { get; set; }
        public int RetryCount { get; set; }
        public DateTime FirstAttemptDate { get; set; }
        public DateTime LastAttemptDate { get; set; }
    }
}
