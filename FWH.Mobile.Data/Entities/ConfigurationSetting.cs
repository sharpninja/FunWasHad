namespace FWH.Mobile.Data.Entities;

/// <summary>
/// Represents a configuration setting stored in the database.
/// </summary>
public class ConfigurationSetting
{
    /// <summary>
    /// The unique key for this configuration setting.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value of the configuration setting.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The data type of the value (e.g., "int", "string", "bool").
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// Optional category for grouping related settings.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional description of what this setting controls.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this setting was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
