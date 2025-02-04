

namespace Entities_Dtos.DTOs;

public class UnrecoupedChargesReportDto
{
    public DateTime AsOfDate { get; set; }
    public decimal TotalUnrecoupedAmount { get; set; }
    public int TotalUnrecoupedCount { get; set; }
    public List<UnrecoupedDetailDto> Details { get; set; }
}
