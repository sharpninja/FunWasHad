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
/// </summary>
public class SubmitFeedbackRequest
{
    public long BusinessId { get; set; }
    public required string UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public required string FeedbackType { get; set; }
    public required string Subject { get; set; }
    public required string Message { get; set; }
    public int? Rating { get; set; }
    public bool IsPublic { get; set; }
    public double? Latitude { get; set; }
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
