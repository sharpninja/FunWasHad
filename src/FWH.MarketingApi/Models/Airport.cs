namespace FWH.MarketingApi.Models;

/// <summary>
/// Represents an airport that can be associated with tourism markets.
/// </summary>
internal class Airport
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string IataCode { get; set; }
    public string? IcaoCode { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string Country { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<AirportTourismMarket> AirportTourismMarkets { get; set; } = new List<AirportTourismMarket>();
}
