namespace FWH.Common.Location.Models;

/// <summary>
/// Represents a business or point of interest location.
/// </summary>
public record BusinessLocation
{
    /// <summary>
    /// Optional business ID (e.g. from Marketing DB or OSM). When null, theme application is skipped.
    /// </summary>
    public long? Id { get; init; }

    /// <summary>
    /// The name of the business or POI.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The full address of the location.
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// The category or type of the business (e.g., restaurant, shop, cafe).
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Additional tags from OpenStreetMap.
    /// </summary>
    public Dictionary<string, string> Tags { get; init; } = new();

    /// <summary>
    /// Distance from the search point in meters (if calculated).
    /// </summary>
    public double? DistanceMeters { get; init; }
}
