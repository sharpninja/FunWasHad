using System.ComponentModel.DataAnnotations;

namespace FWH.MarketingApi.Models;

/// <summary>
/// User feedback submitted to a business.
/// </summary>
public class Feedback
{
    public long Id { get; set; }
    public long BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public required string UserId { get; set; } // Device ID or user account ID
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }

    public required string FeedbackType { get; set; } // "review", "complaint", "suggestion", "compliment"
    public required string Subject { get; set; }
    public required string Message { get; set; }
    public int? Rating { get; set; } // 1-5 stars

    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? BusinessResponse { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    public bool IsPublic { get; set; }
    public bool IsApproved { get; set; }
    public string? ModerationNotes { get; set; }

    // Location context
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Navigation properties
    public ICollection<FeedbackAttachment> Attachments { get; set; } = new List<FeedbackAttachment>();
}

/// <summary>
/// Media attachment for feedback (image or video).
/// </summary>
public class FeedbackAttachment
{
    public long Id { get; set; }
    public long FeedbackId { get; set; }
    public Feedback Feedback { get; set; } = null!;

    public required string AttachmentType { get; set; } // "image", "video"
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string StorageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? DurationSeconds { get; set; } // For videos

    public DateTimeOffset UploadedAt { get; set; }
}

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

/// <summary>
/// Request model for uploading feedback attachments.
/// </summary>
public class UploadAttachmentRequest
{
    public long FeedbackId { get; set; }
    public required string AttachmentType { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] FileData { get; set; }
}

/// <summary>
/// Response model for business marketing data.
/// </summary>
public class BusinessMarketingResponse
{
    public long BusinessId { get; set; }
    public required string BusinessName { get; set; }
    public BusinessTheme? Theme { get; set; }
    public List<Coupon> Coupons { get; set; } = new();
    public List<MenuItem> MenuItems { get; set; } = new();
    public List<NewsItem> NewsItems { get; set; } = new();
}
