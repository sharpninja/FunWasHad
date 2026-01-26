using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Service for managing images and logos, including storage in SQLite database.
/// </summary>
public class ImageService : IImageService
{
    private readonly NotesDbContext _dbContext;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        NotesDbContext dbContext,
        ILogger<ImageService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]?> GetImageAsync(string sourceUrl, string imageType, string? entityType = null, long? entityId = null)
    {
        if (string.IsNullOrEmpty(sourceUrl))
            return null;

        try
        {
            // First, try to get from database
            var existingImage = await _dbContext.Images
                .Where(i => i.SourceUrl == sourceUrl && i.ImageType == imageType)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            if (existingImage != null)
            {
                // Update last accessed time
                existingImage.LastAccessedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogDebug("Retrieved image from cache: {SourceUrl}", sourceUrl);
                return existingImage.ImageData;
            }

            // Not in database, download and store
            if (_httpClientFactory == null)
            {
                _logger.LogWarning("HttpClientFactory not available, cannot download image: {SourceUrl}", sourceUrl);
                return null;
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.GetAsync(new Uri(sourceUrl)).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image: {SourceUrl}, Status: {StatusCode}", sourceUrl, response.StatusCode);
                return null;
            }

            var imageData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

            // Store in database
            var imageId = await StoreImageAsync(
                imageData,
                imageType,
                sourceUrl,
                entityType,
                entityId,
                contentType).ConfigureAwait(false);

            _logger.LogInformation("Downloaded and stored image: {SourceUrl}, Size: {Size} bytes, ImageId: {ImageId}",
                sourceUrl, imageData.Length, imageId);

            return imageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image: {SourceUrl}", sourceUrl);
            return null;
        }
    }

    public async Task<long> StoreImageAsync(
        byte[] imageData,
        string imageType,
        string? sourceUrl = null,
        string? entityType = null,
        long? entityId = null,
        string contentType = "image/png",
        string? fileName = null)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

        try
        {
            // Check if image already exists
            ImageEntity? existingImage = null;
            if (!string.IsNullOrEmpty(sourceUrl))
            {
                existingImage = await _dbContext.Images
                    .Where(i => i.SourceUrl == sourceUrl && i.ImageType == imageType)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
            }
            else if (entityType != null && entityId.HasValue)
            {
                existingImage = await _dbContext.Images
                    .Where(i => i.ImageType == imageType && i.EntityType == entityType && i.EntityId == entityId)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
            }

            if (existingImage != null)
            {
                // Update existing image
                existingImage.ImageData = imageData;
                existingImage.ContentType = contentType;
                existingImage.FileName = fileName;
                existingImage.FileSizeBytes = imageData.Length;
                existingImage.LastAccessedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogDebug("Updated existing image: ImageId: {ImageId}", existingImage.Id);
                return existingImage.Id;
            }

            // Create new image entity
            var image = new ImageEntity
            {
                SourceUrl = sourceUrl,
                ImageType = imageType,
                EntityType = entityType,
                EntityId = entityId,
                ImageData = imageData,
                ContentType = contentType,
                FileName = fileName,
                FileSizeBytes = imageData.Length,
                CreatedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Images.Add(image);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Stored new image: ImageId: {ImageId}, Type: {ImageType}, Size: {Size} bytes",
                image.Id, imageType, imageData.Length);

            return image.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing image: {ImageType}", imageType);
            throw;
        }
    }

    public async Task<byte[]?> GetImageByEntityAsync(string imageType, string entityType, long entityId)
    {
        try
        {
            var image = await _dbContext.Images
                .Where(i => i.ImageType == imageType && i.EntityType == entityType && i.EntityId == entityId)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            if (image != null)
            {
                image.LastAccessedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                return image.ImageData;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image by entity: {ImageType}, {EntityType}, {EntityId}",
                imageType, entityType, entityId);
            return null;
        }
    }

    public async Task DeleteImageAsync(long imageId)
    {
        try
        {
            var image = await _dbContext.Images.FindAsync(imageId).ConfigureAwait(false);
            if (image != null)
            {
                _dbContext.Images.Remove(image);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Deleted image: ImageId: {ImageId}", imageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {ImageId}", imageId);
            throw;
        }
    }

    public async Task<string?> GetImageUriAsync(string sourceUrl, string imageType, string? entityType = null, long? entityId = null)
    {
        var imageData = await GetImageAsync(sourceUrl, imageType, entityType, entityId).ConfigureAwait(false);
        if (imageData == null)
            return null;

        // Get content type from database
        var image = await _dbContext.Images
            .Where(i => i.SourceUrl == sourceUrl && i.ImageType == imageType)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        var contentType = image?.ContentType ?? "image/png";
        var base64 = Convert.ToBase64String(imageData);
        return $"data:{contentType};base64,{base64}";
    }
}
