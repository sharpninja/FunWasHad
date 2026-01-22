using System.Net;
using System.Net.Http.Json;
using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Unit tests for FeedbackController.
/// Implements TR-TEST-001: Unit Tests for API controllers.
/// Implements TR-API-003: Feedback endpoints validation.
/// Implements TR-MEDIA-001 and TR-MEDIA-002: Attachment upload validation.
/// </summary>
public class FeedbackControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FeedbackControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();

        // Clear existing data to avoid conflicts with other test classes
        db.Businesses.RemoveRange(db.Businesses);
        db.Feedbacks.RemoveRange(db.Feedbacks);
        db.FeedbackAttachments.RemoveRange(db.FeedbackAttachments);
        db.SaveChanges();

        // Add test business - MUST be subscribed for feedback submission
        var business = new Business
        {
            Id = 1,
            Name = "Test Business",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true, // Required for feedback submission
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Businesses.Add(business);
        db.SaveChanges();
    }

    /// <summary>
    /// Tests TR-API-003: POST /api/feedback - submits feedback successfully.
    /// Implements TR-API-004: Validation requirements.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The POST /api/feedback endpoint's ability to accept and persist valid feedback submissions.</para>
    /// <para><strong>Data involved:</strong> A SubmitFeedbackRequest with BusinessId=1 (existing subscribed business from SeedTestData), UserId="user-123", UserName="Test User", FeedbackType="review", Subject="Great service", Message="I had a great experience!", Rating=5 (valid 1-5 range), IsPublic=true. All required fields are provided with valid values.</para>
    /// <para><strong>Why the data matters:</strong> Feedback submission is a core feature allowing users to rate and review businesses. The test data includes all required fields with valid values to test the happy path. BusinessId=1 references the seeded test business (which must be subscribed for feedback to work). Rating=5 tests the maximum valid rating, and IsPublic=true tests public feedback visibility.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 201 Created status code with a Feedback response object containing FeedbackType="review" and Rating=5.</para>
    /// <para><strong>Reason for expectation:</strong> When all validation passes (valid business, valid rating range, valid feedback type), the endpoint should create the feedback record in the database and return it with a 201 Created status. The response should contain the submitted data, confirming successful persistence.</para>
    /// </remarks>
    [Fact]
    public async Task SubmitFeedback_ValidRequest_ReturnsCreated()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            UserName = "Test User",
            FeedbackType = "review",
            Subject = "Great service",
            Message = "I had a great experience!",
            Rating = 5,
            IsPublic = true
        };

        var response = await client.PostAsJsonAsync("/api/feedback", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Feedback>();
        Assert.NotNull(result);
        Assert.Equal("review", result!.FeedbackType);
        Assert.Equal(5, result.Rating);
    }

    /// <summary>
    /// Tests TR-API-003: POST /api/feedback - invalid rating returns bad request.
    /// Implements TR-API-004: Validation - rating must be 1-5.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The POST /api/feedback endpoint's input validation for rating values outside the valid range.</para>
    /// <para><strong>Data involved:</strong> A SubmitFeedbackRequest with Rating=6, which exceeds the maximum valid rating of 5. All other fields are valid (BusinessId=1, valid FeedbackType="review", etc.).</para>
    /// <para><strong>Why the data matters:</strong> Rating validation is critical for data integrity - ratings must be in the 1-5 range to be meaningful and consistent. Testing with Rating=6 validates that the endpoint rejects out-of-range values. This prevents invalid data from being stored and ensures consistent rating scales across all feedback.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 400 Bad Request status code.</para>
    /// <para><strong>Reason for expectation:</strong> The endpoint should validate input parameters before processing. Rating values outside the 1-5 range are invalid and should be rejected with a 400 Bad Request, allowing clients to correct the input. This follows REST API best practices for input validation.</para>
    /// </remarks>
    [Fact]
    public async Task SubmitFeedback_InvalidRating_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            FeedbackType = "review",
            Subject = "Test",
            Message = "Test message",
            Rating = 6, // Invalid rating
            IsPublic = true
        };

        var response = await client.PostAsJsonAsync("/api/feedback", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-003: POST /api/feedback - invalid feedback type returns bad request.
    /// </summary>
    [Fact]
    public async Task SubmitFeedback_InvalidFeedbackType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            FeedbackType = "invalid-type",
            Subject = "Test",
            Message = "Test message",
            IsPublic = true
        };

        var response = await client.PostAsJsonAsync("/api/feedback", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-003: POST /api/feedback - non-existent business returns not found.
    /// </summary>
    [Fact]
    public async Task SubmitFeedback_NonExistentBusiness_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new SubmitFeedbackRequest
        {
            BusinessId = 999,
            UserId = "user-123",
            FeedbackType = "review",
            Subject = "Test",
            Message = "Test message",
            IsPublic = true
        };

        var response = await client.PostAsJsonAsync("/api/feedback", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-003: GET /api/feedback/{id} - returns feedback.
    /// </summary>
    [Fact]
    public async Task GetFeedback_ReturnsFeedback()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        // First create feedback
        var createRequest = new SubmitFeedbackRequest
        {
            BusinessId = 1,
            UserId = "user-123",
            FeedbackType = "review",
            Subject = "Test",
            Message = "Test message",
            IsPublic = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/feedback", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Feedback>();
        Assert.NotNull(created);

        // Then retrieve it
        var getResponse = await client.GetAsync($"/api/feedback/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var result = await getResponse.Content.ReadFromJsonAsync<Feedback>();
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
    }

    /// <summary>
    /// Tests TR-API-003: GET /api/feedback/{id} - non-existent feedback returns not found.
    /// </summary>
    [Fact]
    public async Task GetFeedback_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/feedback/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
