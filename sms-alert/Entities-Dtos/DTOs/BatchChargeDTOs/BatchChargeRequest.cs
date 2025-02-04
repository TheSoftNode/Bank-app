namespace Entities_Dtos.DTOs.BatchChargeDTOs;

public class BatchChargeRequest
{
    public required List<AccountChargeDetail> Charges { get; set; }
    public bool ProcessImmediately { get; set; } = false;
}
