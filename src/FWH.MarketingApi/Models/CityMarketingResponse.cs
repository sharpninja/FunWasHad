namespace FWH.MarketingApi.Models;

/// <summary>
/// Response model for city marketing data.
/// </summary>
internal class CityMarketingResponse
{
    public long CityId { get; set; }
    public required string CityName { get; set; }
    public required string State { get; set; }
    public required string Country { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public CityThemeDto? Theme { get; set; }
}
