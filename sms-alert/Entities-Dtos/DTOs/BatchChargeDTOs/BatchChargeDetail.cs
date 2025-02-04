namespace Entities_Dtos.DTOs.BatchChargeDTOs;

public class BatchChargeDetail
{
    public string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public string FailureReason { get; set; }
}
