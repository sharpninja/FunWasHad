using System.ComponentModel.DataAnnotations;

namespace FWH.MarketingApi.Models;

/// <summary>
/// Request model for submitting feedback.
/// Implements TR-API-003: Feedback endpoints request validation.
/// Implements TR-API-004: Validation requirements.
/// </summary>
/// <remarks>
/// Used by the feedback submission endpoint (TR-API-003).
/// All fields are validated according to TR-API-004 and TR-SEC-001.
/// </remarks>
public class SubmitFeedbackRequest
{
    /// <summary>
    /// Business ID to submit feedback for.
    /// </summary>
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Business ID must be greater than 0.")]
    public long BusinessId { get; set; }

    /// <summary>
    /// User ID (device ID or user account ID).
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "User ID must not exceed 100 characters.")]
    public required string UserId { get; set; }

    /// <summary>
    /// Optional user name.
    /// </summary>
    [MaxLength(200, ErrorMessage = "User name must not exceed 200 characters.")]
    public string? UserName { get; set; }

    /// <summary>
    /// Optional user email address.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    public string? UserEmail { get; set; }

    /// <summary>
    /// Feedback type: "review", "complaint", "suggestion", or "compliment".
    /// </summary>
    [Required]
    [RegularExpression("^(review|complaint|suggestion|compliment)$", ErrorMessage = "Feedback type must be one of: review, complaint, suggestion, compliment.")]
    public required string FeedbackType { get; set; }

    /// <summary>
    /// Feedback subject line.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Subject is required.")]
    [MaxLength(200, ErrorMessage = "Subject must not exceed 200 characters.")]
    public required string Subject { get; set; }

    /// <summary>
    /// Feedback message content.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Message is required.")]
    [MaxLength(5000, ErrorMessage = "Message must not exceed 5000 characters.")]
    public required string Message { get; set; }

    /// <summary>
    /// Optional rating (1-5 stars).
    /// Must be between 1 and 5 if provided.
    /// </summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int? Rating { get; set; }

    /// <summary>
    /// Whether the feedback should be publicly visible.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Optional latitude coordinate when feedback was submitted.
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional longitude coordinate when feedback was submitted.
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
    public double? Longitude { get; set; }
}
