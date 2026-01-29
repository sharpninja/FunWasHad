namespace FWH.Mobile.Data.Entities;

/// <summary>
/// Entity for caching city marketing information locally.
/// </summary>
public class CityMarketingInfoEntity
{
    /// <summary>
    /// Primary key for the city marketing info record
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Device identifier (generated GUID)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// City name
    /// </summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// City ID from marketing API (if registered)
    /// </summary>
    public long? CityId { get; set; }

    /// <summary>
    /// City description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// City website
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// City logo URL from marketing API theme
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary color from city theme
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary color from city theme
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Accent color from city theme
    /// </summary>
    public string? AccentColor { get; set; }

    /// <summary>
    /// Background color from city theme
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Text color from city theme
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// Background image URL from city theme
    /// </summary>
    public string? BackgroundImageUrl { get; set; }

    /// <summary>
    /// When this record was created in the database
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When marketing info was last fetched and cached
    /// </summary>
    public DateTimeOffset? MarketingInfoCachedAt { get; set; }
}
