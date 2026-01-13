namespace CorpProcure.DTOs.SystemSetting;

/// <summary>
/// DTO untuk System Setting list/detail
/// </summary>
public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsEditable { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO untuk update setting value
/// </summary>
public class UpdateSystemSettingDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// DTO untuk batch update settings
/// </summary>
public class BatchUpdateSettingsDto
{
    public List<UpdateSystemSettingDto> Settings { get; set; } = new();
}
