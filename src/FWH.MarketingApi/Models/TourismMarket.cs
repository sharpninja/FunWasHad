namespace FWH.MarketingApi.Models;

/// <summary>
/// Represents a tourism market that can contain multiple cities.
/// </summary>
public class TourismMarket
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<CityTourismMarket> CityTourismMarkets { get; set; } = new List<CityTourismMarket>();
    public ICollection<AirportTourismMarket> AirportTourismMarkets { get; set; } = new List<AirportTourismMarket>();
}
