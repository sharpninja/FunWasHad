namespace FWH.Mobile.Services;

/// <summary>
/// Service for managing images and logos, including storage and retrieval.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Gets image data by source URL and type, optionally storing in cache.
    /// </summary>
    Task<byte[]?> GetImageAsync(string sourceUrl, string imageType, string? entityType = null, long? entityId = null);

    /// <summary>
    /// Stores image data and returns the image ID.
    /// </summary>
    Task<long> StoreImageAsync(
        byte[] imageData,
        string imageType,
        string? sourceUrl = null,
        string? entityType = null,
        long? entityId = null,
        string contentType = "image/png",
        string? fileName = null);

    /// <summary>
    /// Gets image data by entity type and ID.
    /// </summary>
    Task<byte[]?> GetImageByEntityAsync(string imageType, string entityType, long entityId);

    /// <summary>
    /// Deletes an image by ID.
    /// </summary>
    Task DeleteImageAsync(long imageId);

    /// <summary>
    /// Gets a data URI for an image (e.g. data:image/png;base64,...).
    /// </summary>
    Task<string?> GetImageUriAsync(string sourceUrl, string imageType, string? entityType = null, long? entityId = null);
}
