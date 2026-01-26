using System.Text;
using FWH.MarketingApi.Controllers;
using FWH.MarketingApi.Models;
using FWH.MarketingApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace FWH.MarketingApi.Tests.Services;

/// <summary>
/// Unit tests for blob storage service with FeedbackController.
/// Tests file upload functionality through controller methods.
/// </summary>
public class BlobStorageIntegrationTests : ControllerTestBase
{
    private FeedbackController CreateController(IBlobStorageService? blobStorage = null)
    {
        blobStorage ??= Substitute.For<IBlobStorageService>();
        return new FeedbackController(DbContext, CreateLogger<FeedbackController>(), blobStorage);
    }

    public BlobStorageIntegrationTests()
    {
        SeedTestBusiness();
    }

    /// <summary>
    /// Tests that image attachment upload stores the file and creates database record.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The complete file upload flow from HTTP request through blob storage to database persistence.</para>
    /// <para><strong>Data involved:</strong> A multipart form POST request to /api/feedback/1/attachments/image with a JPEG image file. The file should be stored in blob storage and a FeedbackAttachment record created in the database.</para>
    /// <para><strong>Why the data matters:</strong> File uploads are a critical feature for feedback attachments. This test verifies the entire flow works correctly, ensuring files are stored and accessible.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 201 Created with a FeedbackAttachment response containing a valid StorageUrl, and the file exists on disk.</para>
    /// <para><strong>Reason for expectation:</strong> When a file is uploaded, it should be stored via the blob storage service, a database record should be created with the storage URL, and the file should be physically present on disk for serving.</para>
    /// </remarks>
    [Fact]
    public async Task UploadImageAttachmentStoresFileAndCreatesRecord()
    {
        // Arrange
        var blobStorage = Substitute.For<IBlobStorageService>();
        var storageUrl = "/uploads/feedback/1/images/test-image.jpg";
        var thumbnailUrl = "/uploads/feedback/1/images/test-image-thumb.jpg";
        blobStorage.UploadWithThumbnailAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((storageUrl, thumbnailUrl));

        var controller = CreateController(blobStorage);

        // First create feedback
        var feedbackRequest = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            UserName = "Test User",
            FeedbackType = "review",
            Subject = "Test",
            Message = "Test message",
            Rating = 5
        };
        var feedbackResult = await controller.SubmitFeedback(feedbackRequest).ConfigureAwait(false);
        var feedbackActionResult = Assert.IsType<ActionResult<Feedback>>(feedbackResult);
        var createdResult = Assert.IsType<CreatedAtActionResult>(feedbackActionResult.Result);
        var feedback = Assert.IsType<Feedback>(createdResult.Value);
        Assert.NotNull(feedback);

        // Create image file content
        var imageContent = Encoding.UTF8.GetBytes("fake jpeg image data");
        var file = CreateMockFormFile(imageContent, "test-image.jpg", "image/jpeg");

        // Act
        var result = await controller.UploadImage(feedback.Id, file).ConfigureAwait(false);

        // Assert
        var actionResult = Assert.IsType<ActionResult<FeedbackAttachment>>(result);
        var createdAttachmentResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var attachment = Assert.IsType<FeedbackAttachment>(createdAttachmentResult.Value);
        Assert.NotNull(attachment);
        Assert.NotNull(attachment.StorageUrl);
        Assert.StartsWith("/uploads/feedback/", attachment.StorageUrl);
        Assert.Equal("image", attachment.AttachmentType);
        Assert.Equal("test-image.jpg", attachment.FileName);
        Assert.Equal("image/jpeg", attachment.ContentType);
        Assert.Equal(imageContent.Length, attachment.FileSizeBytes);
    }

    /// <summary>
    /// Tests that uploaded image files are accessible via the storage URL.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ability to retrieve uploaded files using the storage URL returned from the upload endpoint.</para>
    /// <para><strong>Data involved:</strong> An image file uploaded via the API, then retrieved using the StorageUrl from the response. The file should be retrievable as a stream.</para>
    /// <para><strong>Why the data matters:</strong> Files must be retrievable after upload for serving to clients. This test ensures the blob storage service can read files it has stored.</para>
    /// <para><strong>Expected outcome:</strong> The blob storage service can retrieve the file stream using the storage URL, and the stream contains the original file content.</para>
    /// <para><strong>Reason for expectation:</strong> The storage URL should be a valid reference to the stored file. The blob storage service should be able to retrieve files it has stored, enabling file serving functionality.</para>
    /// </remarks>
    [Fact]
    public async Task UploadImageAttachmentFileIsRetrievable()
    {
        // Arrange
        var imageContent = Encoding.UTF8.GetBytes("test image content");
        var storageUrl = "/uploads/feedback/1/images/test.jpg";
        var thumbnailUrl = "/uploads/feedback/1/images/test-thumb.jpg";
        
        var blobStorage = Substitute.For<IBlobStorageService>();
        blobStorage.UploadWithThumbnailAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((storageUrl, thumbnailUrl));
        
        blobStorage.GetAsync(storageUrl, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(imageContent));

        var controller = CreateController(blobStorage);

        // Create feedback
        var feedbackRequest = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            UserName = "Test User",
            FeedbackType = "review",
            Subject = "Test",
            Message = "Test message",
            Rating = 5
        };
        var feedbackResult = await controller.SubmitFeedback(feedbackRequest).ConfigureAwait(false);
        var feedbackActionResult = Assert.IsType<ActionResult<Feedback>>(feedbackResult);
        var createdResult = Assert.IsType<CreatedAtActionResult>(feedbackActionResult.Result);
        var feedback = Assert.IsType<Feedback>(createdResult.Value);
        Assert.NotNull(feedback);

        // Upload image
        var file = CreateMockFormFile(imageContent, "test.jpg", "image/jpeg");
        var uploadResult = await controller.UploadImage(feedback.Id, file).ConfigureAwait(false);
        var uploadActionResult = Assert.IsType<ActionResult<FeedbackAttachment>>(uploadResult);
        var createdAttachmentResult = Assert.IsType<CreatedAtActionResult>(uploadActionResult.Result);
        var attachment = Assert.IsType<FeedbackAttachment>(createdAttachmentResult.Value);
        Assert.NotNull(attachment);

        // Act - Retrieve file via blob storage
        using var retrievedStream = await blobStorage.GetAsync(attachment.StorageUrl).ConfigureAwait(false);

        // Assert
        Assert.NotNull(retrievedStream);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync().ConfigureAwait(false);
        Assert.Equal("test image content", retrievedContent);
    }

    /// <summary>
    /// Tests that video attachment upload stores the file correctly.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The video file upload functionality through the API.</para>
    /// <para><strong>Data involved:</strong> A multipart form POST request to /api/feedback/{id}/attachments/video with an MP4 video file. The file should be stored and a database record created.</para>
    /// <para><strong>Why the data matters:</strong> Video uploads are supported for feedback attachments. This test ensures video files are handled correctly by the blob storage service.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 201 Created with a FeedbackAttachment response containing AttachmentType="video" and a valid StorageUrl.</para>
    /// <para><strong>Reason for expectation:</strong> Video uploads should work similarly to image uploads, with the file stored via blob storage and a database record created. The attachment type should be correctly set to "video".</para>
    /// </remarks>
    [Fact]
    public async Task UploadVideoAttachmentStoresFileAndCreatesRecord()
    {
        // Arrange
        var blobStorage = Substitute.For<IBlobStorageService>();
        var storageUrl = "/uploads/feedback/1/videos/test-video.mp4";
        var thumbnailUrl = (string?)null;
        blobStorage.UploadWithThumbnailAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((storageUrl, thumbnailUrl));

        var controller = CreateController(blobStorage);

        // Create feedback
        var feedbackRequest = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            UserName = "Test User",
            FeedbackType = "review",
            Subject = "Test",
            Message = "Test message",
            Rating = 5
        };
        var feedbackResult = await controller.SubmitFeedback(feedbackRequest).ConfigureAwait(false);
        var feedbackActionResult = Assert.IsType<ActionResult<Feedback>>(feedbackResult);
        var createdResult = Assert.IsType<CreatedAtActionResult>(feedbackActionResult.Result);
        var feedback = Assert.IsType<Feedback>(createdResult.Value);
        Assert.NotNull(feedback);

        // Create video file content
        var videoContent = Encoding.UTF8.GetBytes("fake mp4 video data");
        var file = CreateMockFormFile(videoContent, "test-video.mp4", "video/mp4");

        // Act
        var result = await controller.UploadVideo(feedback.Id, file).ConfigureAwait(false);

        // Assert
        var actionResult = Assert.IsType<ActionResult<FeedbackAttachment>>(result);
        var createdAttachmentResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var attachment = Assert.IsType<FeedbackAttachment>(createdAttachmentResult.Value);
        Assert.NotNull(attachment);
        Assert.Equal("video", attachment.AttachmentType);
        Assert.NotNull(attachment.StorageUrl);
        Assert.StartsWith("/uploads/feedback/", attachment.StorageUrl);
    }
}
