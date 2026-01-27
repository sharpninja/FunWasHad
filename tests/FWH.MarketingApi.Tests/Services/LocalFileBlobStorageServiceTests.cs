using System.Text;
using FWH.MarketingApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FWH.MarketingApi.Tests.Services;

/// <summary>
/// Unit tests for LocalFileBlobStorageService.
/// Tests the local file system implementation of blob storage for file uploads.
/// </summary>
public class LocalFileBlobStorageServiceTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalFileBlobStorageService> _logger;
    private readonly LocalFileBlobStorageService _service;

    public LocalFileBlobStorageServiceTests()
    {
        // Create a unique test directory for each test run
        _testBasePath = Path.Combine(Path.GetTempPath(), $"blob-storage-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testBasePath);

        _configuration = Substitute.For<IConfiguration>();
        _configuration["BlobStorage:LocalPath"].Returns(_testBasePath);
        _configuration["BlobStorage:BaseUrl"].Returns("/uploads");

        _logger = Substitute.For<ILogger<LocalFileBlobStorageService>>();
        _service = new LocalFileBlobStorageService(_configuration, _logger);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testBasePath))
        {
            try
            {
                Directory.Delete(_testBasePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Tests that UploadAsync stores a file and returns a valid storage URL.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to upload files to local storage and generate accessible URLs.</para>
    /// <para><strong>Data involved:</strong> A text file with content "test content", filename "test.txt", content type "text/plain", and container "test-container". The file should be stored in the configured base path.</para>
    /// <para><strong>Why the data matters:</strong> File upload is the core functionality of blob storage. This test verifies that files are correctly stored and can be accessed via the returned URL.</para>
    /// <para><strong>Expected outcome:</strong> The method returns a storage URL starting with "/uploads/test-container/", and the file exists at the expected location on disk.</para>
    /// <para><strong>Reason for expectation:</strong> The service should store files in container-specific directories and return URLs that match the BaseUrl configuration. The file should be physically present on disk for verification.</para>
    /// </remarks>
    [Fact]
    public async Task UploadAsyncStoresFileAndReturnsUrl()
    {
        // Arrange
        var content = "test content";
        var fileName = "test.txt";
        var contentType = "text/plain";
        var container = "test-container";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var storageUrl = await _service.UploadAsync(stream, fileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(storageUrl);
        Assert.StartsWith("/uploads/test-container/", storageUrl);

        // Verify file exists on disk
        var fileNameFromUrl = Path.GetFileName(storageUrl);
        var expectedPath = Path.Combine(_testBasePath, container, fileNameFromUrl);
        Assert.True(File.Exists(expectedPath));

        // Verify file content
        var fileContent = await File.ReadAllTextAsync(expectedPath, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(content, fileContent);
    }

    /// <summary>
    /// Tests that UploadAsync generates unique file names to prevent conflicts.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's file naming strategy to prevent overwriting existing files.</para>
    /// <para><strong>Data involved:</strong> Two uploads with the same filename "test.txt" to the same container. Each upload should result in a different storage URL.</para>
    /// <para><strong>Why the data matters:</strong> Multiple users might upload files with the same name. Unique file names prevent data loss from overwrites and ensure all uploads are preserved.</para>
    /// <para><strong>Expected outcome:</strong> Both uploads return different storage URLs, and both files exist on disk with different names.</para>
    /// <para><strong>Reason for expectation:</strong> The service prefixes filenames with GUIDs to ensure uniqueness. This prevents conflicts when multiple files share the same original name.</para>
    /// </remarks>
    [Fact]
    public async Task UploadAsyncGeneratesUniqueFileNames()
    {
        // Arrange
        var fileName = "test.txt";
        var contentType = "text/plain";
        var container = "test-container";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("content1"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("content2"));

        // Act
        var url1 = await _service.UploadAsync(stream1, fileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var url2 = await _service.UploadAsync(stream2, fileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotEqual(url1, url2);

        // Both files should exist
        var file1Path = Path.Combine(_testBasePath, container, Path.GetFileName(url1));
        var file2Path = Path.Combine(_testBasePath, container, Path.GetFileName(url2));
        Assert.True(File.Exists(file1Path));
        Assert.True(File.Exists(file2Path));
    }

    /// <summary>
    /// Tests that UploadAsync sanitizes file names to prevent directory traversal attacks.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's security mechanism to prevent malicious file names from accessing directories outside the storage path.</para>
    /// <para><strong>Data involved:</strong> A file with a malicious name "../../../etc/passwd" that attempts directory traversal. The service should sanitize this filename.</para>
    /// <para><strong>Why the data matters:</strong> Directory traversal attacks could allow attackers to overwrite system files or access sensitive data. File name sanitization is critical for security.</para>
    /// <para><strong>Expected outcome:</strong> The file is stored safely within the container directory, and the malicious path components are removed from the filename.</para>
    /// <para><strong>Reason for expectation:</strong> The service should sanitize file names by removing path separators and invalid characters, preventing directory traversal attacks and ensuring files stay within the intended storage location.</para>
    /// </remarks>
    [Fact]
    public async Task UploadAsyncSanitizesFileName()
    {
        // Arrange
        var maliciousFileName = "../../../etc/passwd";
        var contentType = "text/plain";
        var container = "test-container";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act
        var storageUrl = await _service.UploadAsync(stream, maliciousFileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        // Verify the file is stored in the container directory, not outside
        var fileName = Path.GetFileName(storageUrl);
        var filePath = Path.Combine(_testBasePath, container, fileName);
        Assert.True(File.Exists(filePath));

        // Verify the path doesn't contain directory traversal
        Assert.DoesNotContain("..", filePath);
        Assert.StartsWith(_testBasePath, Path.GetFullPath(filePath));
    }

    /// <summary>
    /// Tests that DeleteAsync removes files from storage.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to delete files from storage.</para>
    /// <para><strong>Data involved:</strong> A file uploaded via UploadAsync, then deleted using the returned storage URL. The file should no longer exist after deletion.</para>
    /// <para><strong>Why the data matters:</strong> File deletion is required for cleanup, user data removal, and storage management. This test ensures the delete operation works correctly.</para>
    /// <para><strong>Expected outcome:</strong> DeleteAsync returns true, and the file no longer exists on disk.</para>
    /// <para><strong>Reason for expectation:</strong> The service should remove files from disk when DeleteAsync is called. The method returns true to indicate successful deletion.</para>
    /// </remarks>
    [Fact]
    public async Task DeleteAsyncRemovesFile()
    {
        // Arrange
        var fileName = "test.txt";
        var contentType = "text/plain";
        var container = "test-container";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var storageUrl = await _service.UploadAsync(stream, fileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Verify file exists
        var filePath = Path.Combine(_testBasePath, container, Path.GetFileName(storageUrl));
        Assert.True(File.Exists(filePath));

        // Act
        var deleted = await _service.DeleteAsync(storageUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.True(deleted);
        Assert.False(File.Exists(filePath));
    }

    /// <summary>
    /// Tests that DeleteAsync returns false for non-existent files.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's handling of delete operations on files that don't exist.</para>
    /// <para><strong>Data involved:</strong> A storage URL for a file that was never uploaded or has already been deleted. The service should handle this gracefully.</para>
    /// <para><strong>Why the data matters:</strong> Attempting to delete non-existent files is a common scenario. The service should handle this without throwing exceptions.</para>
    /// <para><strong>Expected outcome:</strong> DeleteAsync returns false without throwing an exception.</para>
    /// <para><strong>Reason for expectation:</strong> The service should gracefully handle missing files by returning false rather than throwing exceptions. This allows callers to handle the case appropriately.</para>
    /// </remarks>
    [Fact]
    public async Task DeleteAsyncNonExistentFileReturnsFalse()
    {
        // Arrange
        var nonExistentUrl = "/uploads/test-container/nonexistent-file.txt";

        // Act
        var deleted = await _service.DeleteAsync(nonExistentUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.False(deleted);
    }

    /// <summary>
    /// Tests that GetAsync retrieves file streams from storage.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to retrieve files from storage as streams.</para>
    /// <para><strong>Data involved:</strong> A file uploaded with content "test content", then retrieved using the storage URL. The retrieved stream should contain the original content.</para>
    /// <para><strong>Why the data matters:</strong> File retrieval is essential for serving files to clients. This test ensures files can be read back correctly after storage.</para>
    /// <para><strong>Expected outcome:</strong> GetAsync returns a stream containing the original file content.</para>
    /// <para><strong>Reason for expectation:</strong> The service should be able to read files from disk and return them as streams. The stream content should match what was originally uploaded.</para>
    /// </remarks>
    [Fact]
    public async Task GetAsyncRetrievesFileStream()
    {
        // Arrange
        var content = "test content";
        var fileName = "test.txt";
        var contentType = "text/plain";
        var container = "test-container";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var storageUrl = await _service.UploadAsync(uploadStream, fileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Act
        using var retrievedStream = await _service.GetAsync(storageUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(retrievedStream);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(content, retrievedContent);
    }

    /// <summary>
    /// Tests that GetAsync returns null for non-existent files.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's handling of retrieval operations for files that don't exist.</para>
    /// <para><strong>Data involved:</strong> A storage URL for a file that was never uploaded. The service should return null rather than throwing an exception.</para>
    /// <para><strong>Why the data matters:</strong> Attempting to retrieve non-existent files is common. The service should handle this gracefully without exceptions.</para>
    /// <para><strong>Expected outcome:</strong> GetAsync returns null without throwing an exception.</para>
    /// <para><strong>Reason for expectation:</strong> The service should return null for missing files, allowing callers to handle the case appropriately (e.g., return 404 to clients).</para>
    /// </remarks>
    [Fact]
    public async Task GetAsyncNonExistentFileReturnsNull()
    {
        // Arrange
        var nonExistentUrl = "/uploads/test-container/nonexistent-file.txt";

        // Act
        var stream = await _service.GetAsync(nonExistentUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Null(stream);
    }

    /// <summary>
    /// Tests that ExistsAsync correctly identifies existing files.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to check file existence without reading the file.</para>
    /// <para><strong>Data involved:</strong> A file uploaded via UploadAsync, then checked for existence using the storage URL. The method should return true.</para>
    /// <para><strong>Why the data matters:</strong> File existence checks are useful for validation and optimization. This test ensures the check works correctly.</para>
    /// <para><strong>Expected outcome:</strong> ExistsAsync returns true for the uploaded file.</para>
    /// <para><strong>Reason for expectation:</strong> The service should accurately report file existence by checking the file system. This is useful for validation before operations.</para>
    /// </remarks>
    [Fact]
    public async Task ExistsAsyncExistingFileReturnsTrue()
    {
        // Arrange
        var fileName = "test.txt";
        var contentType = "text/plain";
        var container = "test-container";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var storageUrl = await _service.UploadAsync(stream, fileName, contentType, container, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Act
        var exists = await _service.ExistsAsync(storageUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.True(exists);
    }

    /// <summary>
    /// Tests that ExistsAsync correctly identifies non-existent files.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to correctly report when files don't exist.</para>
    /// <para><strong>Data involved:</strong> A storage URL for a file that was never uploaded. The method should return false.</para>
    /// <para><strong>Why the data matters:</strong> Accurate existence checks prevent errors when attempting to access missing files. This test ensures the check correctly identifies missing files.</para>
    /// <para><strong>Expected outcome:</strong> ExistsAsync returns false for the non-existent file.</para>
    /// <para><strong>Reason for expectation:</strong> The service should accurately report when files don't exist, allowing callers to handle missing files appropriately.</para>
    /// </remarks>
    [Fact]
    public async Task ExistsAsyncNonExistentFileReturnsFalse()
    {
        // Arrange
        var nonExistentUrl = "/uploads/test-container/nonexistent-file.txt";

        // Act
        var exists = await _service.ExistsAsync(nonExistentUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.False(exists);
    }

    /// <summary>
    /// Tests that UploadAsync throws ArgumentNullException when stream is null.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's validation of required parameters.</para>
    /// <para><strong>Data involved:</strong> null value for fileStream parameter. The service requires a valid stream to upload.</para>
    /// <para><strong>Why the data matters:</strong> Null parameters would cause runtime errors. This test ensures the service validates inputs and provides clear error messages.</para>
    /// <para><strong>Expected outcome:</strong> ArgumentNullException is thrown with parameter name "fileStream".</para>
    /// <para><strong>Reason for expectation:</strong> The service should validate required parameters at the start of the method, failing fast with clear error messages rather than failing later with cryptic errors.</para>
    /// </remarks>
    [Fact]
    public async Task UploadAsyncNullStreamThrowsArgumentNullException()
    {
        Stream? stream = null;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UploadAsync(stream!, "test.txt", "text/plain", "container", TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Tests that UploadAsync throws ArgumentException when file name is empty.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's validation of file name parameter.</para>
    /// <para><strong>Data involved:</strong> Empty string for fileName parameter. The service requires a valid file name to generate storage paths.</para>
    /// <para><strong>Why the data matters:</strong> Empty file names would cause invalid file paths. This test ensures the service validates this requirement.</para>
    /// <para><strong>Expected outcome:</strong> ArgumentException is thrown indicating file name cannot be null or empty.</para>
    /// <para><strong>Reason for expectation:</strong> The service should validate that file names are provided, as they're required for generating storage paths and URLs.</para>
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UploadAsyncInvalidFileNameThrowsArgumentException(string? fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadAsync(stream, fileName!, "text/plain", "container", TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Tests that UploadWithThumbnailAsync returns both storage URL and thumbnail URL.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to upload files and optionally generate thumbnails.</para>
    /// <para><strong>Data involved:</strong> An image file upload with generateThumbnail=true. The method should return both the main file URL and a thumbnail URL.</para>
    /// <para><strong>Why the data matters:</strong> Thumbnails are useful for displaying image previews. This test verifies the thumbnail generation path works (even if not fully implemented yet).</para>
    /// <para><strong>Expected outcome:</strong> The method returns a tuple with both StorageUrl and ThumbnailUrl (may be null if not implemented).</para>
    /// <para><strong>Reason for expectation:</strong> The service should support thumbnail generation for images. Currently, thumbnails may be null if not implemented, but the method signature should support it.</para>
    /// </remarks>
    [Fact]
    public async Task UploadWithThumbnailAsyncReturnsStorageAndThumbnailUrls()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var container = "test-container";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake image content"));

        // Act
        var (storageUrl, thumbnailUrl) = await _service.UploadWithThumbnailAsync(
            stream, fileName, contentType, container, generateThumbnail: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(storageUrl);
        Assert.StartsWith("/uploads/test-container/", storageUrl);
        // Thumbnail may be null if not implemented yet
    }
}
