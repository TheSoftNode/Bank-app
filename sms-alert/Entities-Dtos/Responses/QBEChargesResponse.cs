namespace Entities_Dtos.Responses;

public class QBEChargesResponse
{
    public string CustomerNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCharges { get; set; }
}