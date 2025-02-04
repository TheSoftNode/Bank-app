namespace Entities_Dtos.DTOs.Config;

public class CreateConfigRequest
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Description { get; set; }
}
