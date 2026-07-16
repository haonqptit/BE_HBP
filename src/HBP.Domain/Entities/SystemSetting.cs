namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>system_settings</c> (string primary key <c>key</c>).</summary>
public class SystemSetting
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}
