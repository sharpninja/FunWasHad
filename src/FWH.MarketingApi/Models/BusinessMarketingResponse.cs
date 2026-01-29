namespace FWH.MarketingApi.Models;

/// <summary>
/// Response model for business marketing data.
/// </summary>
internal class BusinessMarketingResponse
{
    public long BusinessId { get; set; }
    public required string BusinessName { get; set; }
    public BusinessTheme? Theme { get; set; }
    public List<Coupon> Coupons { get; set; } = new();
    public List<MenuItem> MenuItems { get; set; } = new();
    public List<NewsItem> NewsItems { get; set; } = new();
}
