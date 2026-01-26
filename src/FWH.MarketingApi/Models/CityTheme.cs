namespace FWH.MarketingApi.Models;

/// <summary>
/// Theme settings for customizing the app appearance when in a city.
/// </summary>
internal class CityTheme
{
    public long Id { get; set; }
    public long CityId { get; set; }
    public City City { get; set; } = null!;

    public required string ThemeName { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public string? LogoUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? CustomCss { get; set; }

    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
