namespace FWH.MarketingApi.Models;

/// <summary>
/// Join entity for the many-to-many relationship between City and TourismMarket.
/// </summary>
public class CityTourismMarket
{
    public long Id { get; set; }
    public long CityId { get; set; }
    public City City { get; set; } = null!;
    public long TourismMarketId { get; set; }
    public TourismMarket TourismMarket { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
