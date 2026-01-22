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
