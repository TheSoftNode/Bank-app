using System.ComponentModel.DataAnnotations;

namespace Entities_Dtos.DTOs.BalanceEnquiryDTOs;

public class QBEProcessingDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
