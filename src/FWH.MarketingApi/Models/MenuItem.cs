namespace FWH.MarketingApi.Models;

/// <summary>
/// Menu item from a business (e.g., restaurant menu).
/// </summary>
internal class MenuItem
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
