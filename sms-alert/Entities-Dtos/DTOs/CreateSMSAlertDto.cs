namespace Entities_Dtos.DTOs
{
    public class CreateSMSAlertDto
    {
        public string Email { get; set; }
        public string AccountNumber { get; set; }
        public string MessageContent { get; set; }
        public string AlertType { get; set; }
        public string PhoneNumber { get; set; }
    }
}
