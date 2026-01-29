namespace FWH.MarketingApi.Models;

/// <summary>
/// News item or announcement from a business.
/// </summary>
internal class NewsItem
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
