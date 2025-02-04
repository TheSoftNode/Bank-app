namespace Entities_Dtos.DTOs;

public class AlertChargeSearchDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Email { get; set; }
    public string AccountNumber { get; set; }
    public string ChargeStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
