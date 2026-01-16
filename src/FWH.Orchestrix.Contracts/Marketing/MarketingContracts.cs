using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Orchestrix.Contracts.Marketing;

/// <summary>
/// Base response for marketing operations.
/// </summary>
public record MarketingResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Request to get business marketing data.
/// </summary>
public record GetBusinessMarketingRequest : IMediatorRequest<GetBusinessMarketingResponse>
{
    public long BusinessId { get; init; }
}

/// <summary>
/// Response to get business marketing data.
/// </summary>
public record GetBusinessMarketingResponse : MarketingResponse
{
    public BusinessMarketingDto? Data { get; init; }
}

/// <summary>
/// Request to get business theme.
/// </summary>
public record GetBusinessThemeRequest : IMediatorRequest<GetBusinessThemeResponse>
{
    public long BusinessId { get; init; }
}

/// <summary>
/// Response to get business theme.
/// </summary>
public record GetBusinessThemeResponse : MarketingResponse
{
    public BusinessThemeDto? Theme { get; init; }
}

/// <summary>
/// Request to get business coupons.
/// </summary>
public record GetBusinessCouponsRequest : IMediatorRequest<GetBusinessCouponsResponse>
{
    public long BusinessId { get; init; }
}

/// <summary>
/// Response to get business coupons.
/// </summary>
public record GetBusinessCouponsResponse : MarketingResponse
{
    public List<CouponDto> Coupons { get; init; } = new();
}

/// <summary>
/// Request to get business menu.
/// </summary>
public record GetBusinessMenuRequest : IMediatorRequest<GetBusinessMenuResponse>
{
    public long BusinessId { get; init; }
    public string? Category { get; init; }
}

/// <summary>
/// Response to get business menu.
/// </summary>
public record GetBusinessMenuResponse : MarketingResponse
{
    public List<MenuItemDto> MenuItems { get; init; } = new();
}

/// <summary>
/// Request to get business news.
/// </summary>
public record GetBusinessNewsRequest : IMediatorRequest<GetBusinessNewsResponse>
{
    public long BusinessId { get; init; }
    public int Limit { get; init; } = 10;
}

/// <summary>
/// Response to get business news.
/// </summary>
public record GetBusinessNewsResponse : MarketingResponse
{
    public List<NewsItemDto> NewsItems { get; init; } = new();
}

/// <summary>
/// Request to submit feedback.
/// </summary>
public record SubmitFeedbackRequest : IMediatorRequest<SubmitFeedbackResponse>
{
    public long BusinessId { get; init; }
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public required string FeedbackType { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public int? Rating { get; init; }
    public bool IsPublic { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

/// <summary>
/// Response to submit feedback.
/// </summary>
public record SubmitFeedbackResponse : MarketingResponse
{
    public long? FeedbackId { get; init; }
}

/// <summary>
/// Request to upload feedback attachment.
/// </summary>
public record UploadFeedbackAttachmentRequest : IMediatorRequest<UploadFeedbackAttachmentResponse>
{
    public long FeedbackId { get; init; }
    public required string AttachmentType { get; init; } // "image" or "video"
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] FileData { get; init; }
}

/// <summary>
/// Response to upload feedback attachment.
/// </summary>
public record UploadFeedbackAttachmentResponse : MarketingResponse
{
    public long? AttachmentId { get; init; }
    public string? StorageUrl { get; init; }
}

/// <summary>
/// Request to find nearby businesses.
/// </summary>
public record FindNearbyBusinessesRequest : IMediatorRequest<FindNearbyBusinessesResponse>
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int RadiusMeters { get; init; } = 1000;
}

/// <summary>
/// Response to find nearby businesses.
/// </summary>
public record FindNearbyBusinessesResponse : MarketingResponse
{
    public List<BusinessSummaryDto> Businesses { get; init; } = new();
}

// DTOs

/// <summary>
/// Complete business marketing data.
/// </summary>
public record BusinessMarketingDto
{
    public long BusinessId { get; init; }
    public required string BusinessName { get; init; }
    public BusinessThemeDto? Theme { get; init; }
    public List<CouponDto> Coupons { get; init; } = new();
    public List<MenuItemDto> MenuItems { get; init; } = new();
    public List<NewsItemDto> NewsItems { get; init; } = new();
}

/// <summary>
/// Business theme data.
/// </summary>
public record BusinessThemeDto
{
    public long Id { get; init; }
    public long BusinessId { get; init; }
    public required string ThemeName { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? AccentColor { get; init; }
    public string? BackgroundColor { get; init; }
    public string? TextColor { get; init; }
    public string? LogoUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? CustomCss { get; init; }
}

/// <summary>
/// Coupon data.
/// </summary>
public record CouponDto
{
    public long Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? Code { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public string? ImageUrl { get; init; }
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset ValidUntil { get; init; }
    public int? MaxRedemptions { get; init; }
    public int CurrentRedemptions { get; init; }
}

/// <summary>
/// Menu item data.
/// </summary>
public record MenuItemDto
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Category { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public string? ImageUrl { get; init; }
    public int? Calories { get; init; }
    public string? Allergens { get; init; }
    public string? DietaryTags { get; init; }
}

/// <summary>
/// News item data.
/// </summary>
public record NewsItemDto
{
    public long Id { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string? Summary { get; init; }
    public string? ImageUrl { get; init; }
    public string? Author { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public bool IsFeatured { get; init; }
}

/// <summary>
/// Business summary data.
/// </summary>
public record BusinessSummaryDto
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? DistanceMeters { get; init; }
    public bool IsSubscribed { get; init; }
}
