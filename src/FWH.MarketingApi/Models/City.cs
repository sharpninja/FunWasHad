namespace FWH.MarketingApi.Models;

/// <summary>
/// Represents a city that can have marketing information (theme, logo, general info).
/// </summary>
public class City
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string State { get; set; }
    public required string Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public CityTheme? Theme { get; set; }
    public ICollection<CityTourismMarket> CityTourismMarkets { get; set; } = new List<CityTourismMarket>();
}
