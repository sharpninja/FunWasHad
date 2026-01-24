using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using FWH.MarketingApi.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.MarketingApi.Tests.Services;

/// <summary>
/// Integration tests for blob storage service with FeedbackController.
/// Tests file upload functionality end-to-end through the HTTP API.
/// </summary>
public class BlobStorageIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BlobStorageIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();

        // Clear existing data
        db.Businesses.RemoveRange(db.Businesses);
        db.Feedbacks.RemoveRange(db.Feedbacks);
        db.FeedbackAttachments.RemoveRange(db.FeedbackAttachments);
        db.SaveChanges();

        // Add test business
        var business = new Business
        {
            Id = 1,
            Name = "Test Business",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Businesses.Add(business);
        db.SaveChanges();
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
    public async Task UploadImageAttachment_StoresFileAndCreatesRecord()
    {
        // Arrange
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

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
        var feedbackResponse = await client.PostAsJsonAsync("/api/feedback", feedbackRequest);
        var feedback = await feedbackResponse.Content.ReadFromJsonAsync<Feedback>();
        Assert.NotNull(feedback);

        // Create image file content
        var imageContent = Encoding.UTF8.GetBytes("fake jpeg image data");
        using var content = new MultipartFormDataContent();
        using var imageStream = new MemoryStream(imageContent);
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", "test-image.jpg");

        // Act
        var response = await client.PostAsync($"/api/feedback/{feedback!.Id}/attachments/image", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var attachment = await response.Content.ReadFromJsonAsync<FeedbackAttachment>();
        Assert.NotNull(attachment);
        Assert.NotNull(attachment!.StorageUrl);
        Assert.StartsWith("/uploads/feedback/", attachment.StorageUrl);
        Assert.Equal("image", attachment.AttachmentType);
        Assert.Equal("test-image.jpg", attachment.FileName);
        Assert.Equal("image/jpeg", attachment.ContentType);
        Assert.Equal(imageContent.Length, attachment.FileSizeBytes);

        // Verify file exists via blob storage service
        using var scope = _factory.Services.CreateScope();
        var blobStorage = scope.ServiceProvider.GetRequiredService<FWH.MarketingApi.Services.IBlobStorageService>();
        var exists = await blobStorage.ExistsAsync(attachment.StorageUrl);
        Assert.True(exists);
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
    public async Task UploadImageAttachment_FileIsRetrievable()
    {
        // Arrange
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

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
        var feedbackResponse = await client.PostAsJsonAsync("/api/feedback", feedbackRequest);
        var feedback = await feedbackResponse.Content.ReadFromJsonAsync<Feedback>();
        Assert.NotNull(feedback);

        // Upload image
        var imageContent = Encoding.UTF8.GetBytes("test image content");
        using var content = new MultipartFormDataContent();
        using var imageStream = new MemoryStream(imageContent);
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", "test.jpg");

        var uploadResponse = await client.PostAsync($"/api/feedback/{feedback!.Id}/attachments/image", content);
        var attachment = await uploadResponse.Content.ReadFromJsonAsync<FeedbackAttachment>();
        Assert.NotNull(attachment);

        // Act - Retrieve file via blob storage
        using var scope = _factory.Services.CreateScope();
        var blobStorage = scope.ServiceProvider.GetRequiredService<FWH.MarketingApi.Services.IBlobStorageService>();
        using var retrievedStream = await blobStorage.GetAsync(attachment!.StorageUrl);

        // Assert
        Assert.NotNull(retrievedStream);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
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
    public async Task UploadVideoAttachment_StoresFileAndCreatesRecord()
    {
        // Arrange
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

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
        var feedbackResponse = await client.PostAsJsonAsync("/api/feedback", feedbackRequest);
        var feedback = await feedbackResponse.Content.ReadFromJsonAsync<Feedback>();
        Assert.NotNull(feedback);

        // Create video file content
        var videoContent = Encoding.UTF8.GetBytes("fake mp4 video data");
        using var content = new MultipartFormDataContent();
        using var videoStream = new MemoryStream(videoContent);
        var streamContent = new StreamContent(videoStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
        content.Add(streamContent, "file", "test-video.mp4");

        // Act
        var response = await client.PostAsync($"/api/feedback/{feedback!.Id}/attachments/video", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var attachment = await response.Content.ReadFromJsonAsync<FeedbackAttachment>();
        Assert.NotNull(attachment);
        Assert.Equal("video", attachment!.AttachmentType);
        Assert.NotNull(attachment.StorageUrl);
        Assert.StartsWith("/uploads/feedback/", attachment.StorageUrl);
    }
}
