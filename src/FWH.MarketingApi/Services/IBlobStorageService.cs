namespace FWH.MarketingApi.Services;

/// <summary>
/// Service for storing and retrieving blob files (images, videos, etc.).
/// Abstracts storage implementation to support different backends (local filesystem, cloud storage, etc.).
/// </summary>
internal interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file and returns the storage URL.
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">The original file name</param>
    /// <param name="contentType">The content type (MIME type) of the file</param>
    /// <param name="container">The container/folder name (e.g., "feedback-images", "feedback-videos")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The public URL to access the stored file</returns>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string container,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file and returns both the storage URL and optional thumbnail URL.
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">The original file name</param>
    /// <param name="contentType">The content type (MIME type) of the file</param>
    /// <param name="container">The container/folder name</param>
    /// <param name="generateThumbnail">Whether to generate a thumbnail (for images/videos)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing storage URL and optional thumbnail URL</returns>
    Task<(string StorageUrl, string? ThumbnailUrl)> UploadWithThumbnailAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string container,
        bool generateThumbnail = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="storageUrl">The storage URL of the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was deleted, false if it didn't exist</returns>
    Task<bool> DeleteAsync(string storageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file stream from storage.
    /// </summary>
    /// <param name="storageUrl">The storage URL of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The file stream, or null if the file doesn't exist</returns>
    Task<Stream?> GetAsync(string storageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    /// <param name="storageUrl">The storage URL to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file exists, false otherwise</returns>
    Task<bool> ExistsAsync(string storageUrl, CancellationToken cancellationToken = default);
}
