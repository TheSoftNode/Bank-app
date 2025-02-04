namespace Entities_Dtos.DBSets;

public class SystemConfiguration : BaseEntity
{
    public string ConfigKey { get; set; }
    public string ConfigValue { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    public string LastModifiedBy { get; set; }
}
