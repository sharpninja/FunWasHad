using System;

namespace FWH.Mobile.Data.Entities;

/// <summary>
/// Entity for storing places where the user became stationary.
/// Stores business information when available.
/// </summary>
public class StationaryPlaceEntity
{
    /// <summary>
    /// Primary key for the place record
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Device identifier (generated GUID)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Business name (if a business was found at this location)
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// Full address of the location
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Business category (e.g., restaurant, cafe, shop)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Latitude in decimal degrees
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Timestamp when the user became stationary at this location
    /// </summary>
    public DateTimeOffset StationaryAt { get; set; }

    /// <summary>
    /// When this record was created in the database
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether this place is marked as a favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Business logo URL from marketing API theme
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary color from business theme
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary color from business theme
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Accent color from business theme
    /// </summary>
    public string? AccentColor { get; set; }

    /// <summary>
    /// Background color from business theme
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Text color from business theme
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// Background image URL from business theme
    /// </summary>
    public string? BackgroundImageUrl { get; set; }

    /// <summary>
    /// Business ID from marketing API (if registered)
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// When marketing info was last fetched and cached
    /// </summary>
    public DateTimeOffset? MarketingInfoCachedAt { get; set; }
}
