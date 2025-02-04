using System.ComponentModel.DataAnnotations;

namespace Entities_Dtos.DTOs.BatchProcessing;

public class MonthlyQBEProcessingDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // Optional parameters for additional control
    public bool ConsolidateCharges { get; set; } = true;
    public bool ProcessFailedTransactions { get; set; } = true;
}
