namespace FWH.MarketingApi.Models;

/// <summary>
/// Join entity for the many-to-many relationship between Airport and TourismMarket.
/// </summary>
internal class AirportTourismMarket
{
    public long Id { get; set; }
    public long AirportId { get; set; }
    public Airport Airport { get; set; } = null!;
    public long TourismMarketId { get; set; }
    public TourismMarket TourismMarket { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
