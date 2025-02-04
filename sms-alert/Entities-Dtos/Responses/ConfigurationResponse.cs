namespace Entities_Dtos.Responses;

public class ConfigurationResponseDto
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
    public string LastModifiedBy { get; set; }
    public DateTime LastModifiedDate { get; set; }
}
