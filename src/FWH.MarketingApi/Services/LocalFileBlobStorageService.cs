using System.IO;

namespace FWH.MarketingApi.Services;

/// <summary>
/// Local file system implementation of blob storage.
/// Used for development and can be used for Railway with persistent volumes.
/// </summary>
public class LocalFileBlobStorageService : IBlobStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileBlobStorageService> _logger;

    public LocalFileBlobStorageService(
        IConfiguration configuration,
        ILogger<LocalFileBlobStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(configuration);

        // Get storage path from configuration, default to ./uploads in current directory
        _basePath = configuration["BlobStorage:LocalPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        // Get base URL for serving files, default to /uploads
        _baseUrl = configuration["BlobStorage:BaseUrl"] ?? "/uploads";

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created blob storage directory: {BasePath}", _basePath);
        }

        _logger.LogInformation("Local file blob storage initialized. Base path: {BasePath}, Base URL: {BaseUrl}",
            _basePath, _baseUrl);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string container,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("Container cannot be null or empty", nameof(container));

        // Sanitize file name
        var sanitizedFileName = SanitizeFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";

        // Create container directory
        var containerPath = Path.Combine(_basePath, container);
        if (!Directory.Exists(containerPath))
        {
            Directory.CreateDirectory(containerPath);
        }

        // Full file path
        var filePath = Path.Combine(containerPath, uniqueFileName);

        // Write file
        using (var fileStreamWriter = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(fileStreamWriter, cancellationToken);
        }

        // Return public URL
        var storageUrl = $"{_baseUrl.TrimEnd('/')}/{container}/{uniqueFileName}";
        _logger.LogDebug("Uploaded file to {StorageUrl}", storageUrl);

        return storageUrl;
    }

    public async Task<(string StorageUrl, string? ThumbnailUrl)> UploadWithThumbnailAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string container,
        bool generateThumbnail = false,
        CancellationToken cancellationToken = default)
    {
        var storageUrl = await UploadAsync(fileStream, fileName, contentType, container, cancellationToken);

        // Thumbnail generation would be implemented here for images/videos
        // For now, return null for thumbnail
        string? thumbnailUrl = null;

        if (generateThumbnail && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: Implement thumbnail generation for images
            _logger.LogDebug("Thumbnail generation requested but not yet implemented for {FileName}", fileName);
        }

        return (storageUrl, thumbnailUrl);
    }

    public Task<bool> DeleteAsync(string storageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageUrl))
            return Task.FromResult(false);

        try
        {
            var filePath = StorageUrlToFilePath(storageUrl);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted file: {StorageUrl}", storageUrl);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting file: {StorageUrl}", storageUrl);
        }

        return Task.FromResult(false);
    }

    public Task<Stream?> GetAsync(string storageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageUrl))
            return Task.FromResult<Stream?>(null);

        try
        {
            var filePath = StorageUrlToFilePath(storageUrl);
            if (File.Exists(filePath))
            {
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return Task.FromResult<Stream?>(stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading file: {StorageUrl}", storageUrl);
        }

        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> ExistsAsync(string storageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageUrl))
            return Task.FromResult(false);

        try
        {
            var filePath = StorageUrlToFilePath(storageUrl);
            return Task.FromResult(File.Exists(filePath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string StorageUrlToFilePath(string storageUrl)
    {
        // Remove base URL prefix
        var relativePath = storageUrl;
        if (storageUrl.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = storageUrl.Substring(_baseUrl.Length).TrimStart('/');
        }
        else if (storageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = storageUrl.Substring("/uploads/".Length);
        }

        return Path.Combine(_basePath, relativePath);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove path separators and other dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Remove ".." to prevent directory traversal
        while (sanitized.Contains("..", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("..", "", StringComparison.Ordinal);
        }

        // Limit length
        if (sanitized.Length > 255)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt.Substring(0, Math.Min(255 - extension.Length, nameWithoutExt.Length)) + extension;
        }

        return sanitized;
    }
}
