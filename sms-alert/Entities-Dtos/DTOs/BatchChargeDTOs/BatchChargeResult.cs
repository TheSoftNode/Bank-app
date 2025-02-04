namespace Entities_Dtos.DTOs.BatchChargeDTOs;


public class BatchChargeResult
{
    public int TotalCharges { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ProcessedAmount { get; set; }
    public decimal FailedAmount { get; set; }
    public DateTime ProcessingDate { get; set; }
    public List<BatchChargeDetail> Details { get; set; } = new();
}
