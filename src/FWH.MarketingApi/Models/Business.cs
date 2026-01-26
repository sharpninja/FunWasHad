namespace FWH.MarketingApi.Models;

/// <summary>
/// Represents a business that can advertise through the mobile app.
/// </summary>
internal class Business
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public bool IsSubscribed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubscriptionExpiresAt { get; set; }

    // Navigation properties
    public BusinessTheme? Theme { get; set; }
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<NewsItem> NewsItems { get; set; } = new List<NewsItem>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
}
