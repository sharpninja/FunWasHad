namespace FWH.MarketingApi.Models;

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
