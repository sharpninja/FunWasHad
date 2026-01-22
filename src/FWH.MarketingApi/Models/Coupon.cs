namespace FWH.MarketingApi.Models;

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
