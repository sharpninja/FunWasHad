using System;

namespace FWH.Mobile.Data.Entities;

/// <summary>
/// Entity for storing images and logos in the local SQLite database.
/// </summary>
public class ImageEntity
{
    /// <summary>
    /// Primary key for the image record
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Original URL where the image was retrieved from (for reference)
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Type/category of image (e.g., "business_logo", "city_logo", "background_image", "user_upload")
    /// </summary>
    public string ImageType { get; set; } = string.Empty;

    /// <summary>
    /// Associated entity ID (e.g., BusinessId, CityId, PlaceId)
    /// </summary>
    public long? EntityId { get; set; }

    /// <summary>
    /// Entity type (e.g., "Business", "City", "Place", "User")
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Image data stored as byte array
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// MIME type of the image (e.g., "image/png", "image/jpeg")
    /// </summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>
    /// Original filename (if available)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// When this record was created in the database
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this image was last accessed/used
    /// </summary>
    public DateTimeOffset? LastAccessedAt { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public int FileSizeBytes { get; set; }
}
