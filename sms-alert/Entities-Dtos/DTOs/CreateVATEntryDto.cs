using Entities_Dtos.Types;

namespace Entities_Dtos.DTOs;

public class CreateVATEntryDto
{
    public decimal Amount { get; set; }
    public string CustomerNumber { get; set; }
    public EntryType EntryType { get; set; }
}
