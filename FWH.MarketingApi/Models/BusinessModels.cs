namespace FWH.MarketingApi.Models;

/// <summary>
/// Represents a business that can advertise through the mobile app.
/// </summary>
public class Business
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

/// <summary>
/// Theme settings for customizing the app appearance when at a business location.
/// </summary>
public class BusinessTheme
{
    public long Id { get; set; }
    public long BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    
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

/// <summary>
/// Coupon or promotional offer from a business.
/// </summary>
public class Coupon
{
    public long Id { get; set; }
    public long BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Code { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? ImageUrl { get; set; }
    public string? TermsAndConditions { get; set; }
    
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int CurrentRedemptions { get; set; }
    
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Menu item from a business (e.g., restaurant menu).
/// </summary>
public class MenuItem
{
    public long Id { get; set; }
    public long BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
    
    // Nutritional information (optional)
    public int? Calories { get; set; }
    public string? Allergens { get; set; }
    public string? DietaryTags { get; set; } // e.g., "vegetarian,gluten-free"
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// News item or announcement from a business.
/// </summary>
public class NewsItem
{
    public long Id { get; set; }
    public long BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }
    
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
