using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FWH.MarketingApi.Controllers;

/// <summary>
/// API controller for submitting and managing user feedback to businesses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly MarketingDbContext _context;
    private readonly ILogger<FeedbackController> _logger;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private static readonly string[] AllowedVideoTypes = { "video/mp4", "video/quicktime", "video/x-msvideo" };

    public FeedbackController(MarketingDbContext context, ILogger<FeedbackController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit text feedback to a business.
    /// </summary>
    /// <param name="request">Feedback details</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Feedback>> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        // Validate business exists and is subscribed
        var business = await _context.Businesses.FindAsync(request.BusinessId);
        if (business == null)
        {
            return NotFound($"Business {request.BusinessId} not found");
        }

        if (!business.IsSubscribed)
        {
            return BadRequest("Business is not subscribed to feedback service");
        }

        // Validate feedback type
        var validTypes = new[] { "review", "complaint", "suggestion", "compliment" };
        if (!validTypes.Contains(request.FeedbackType.ToLower()))
        {
            return BadRequest($"Invalid feedback type. Must be one of: {string.Join(", ", validTypes)}");
        }

        // Validate rating if provided
        if (request.Rating.HasValue && (request.Rating < 1 || request.Rating > 5))
        {
            return BadRequest("Rating must be between 1 and 5");
        }

        var feedback = new Feedback
        {
            BusinessId = request.BusinessId,
            UserId = request.UserId,
            UserName = request.UserName,
            UserEmail = request.UserEmail,
            FeedbackType = request.FeedbackType.ToLower(),
            Subject = request.Subject,
            Message = request.Message,
            Rating = request.Rating,
            IsPublic = request.IsPublic,
            IsApproved = false, // Requires moderation
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feedback {FeedbackId} submitted for business {BusinessId} by user {UserId}",
            feedback.Id, request.BusinessId, request.UserId);

        return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, feedback);
    }

    /// <summary>
    /// Upload an image attachment for feedback.
    /// </summary>
    /// <param name="feedbackId">Feedback ID</param>
    /// <param name="file">Image file</param>
    [HttpPost("{feedbackId}/attachments/image")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<FeedbackAttachment>> UploadImage(long feedbackId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest($"File size exceeds maximum of {MaxFileSize / (1024 * 1024)}MB");
        }

        if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest($"Invalid image type. Allowed types: {string.Join(", ", AllowedImageTypes)}");
        }

        var feedback = await _context.Feedbacks.FindAsync(feedbackId);
        if (feedback == null)
        {
            return NotFound($"Feedback {feedbackId} not found");
        }

        // In production, upload to cloud storage (S3, Azure Blob, etc.)
        // For now, simulate storage URL
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var storageUrl = $"/uploads/feedback/{feedbackId}/images/{fileName}";
        var thumbnailUrl = $"/uploads/feedback/{feedbackId}/images/thumb_{fileName}";

        var attachment = new FeedbackAttachment
        {
            FeedbackId = feedbackId,
            AttachmentType = "image",
            FileName = file.FileName,
            ContentType = file.ContentType,
            StorageUrl = storageUrl,
            ThumbnailUrl = thumbnailUrl,
            FileSizeBytes = file.Length,
            UploadedAt = DateTimeOffset.UtcNow
        };

        _context.FeedbackAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Image attachment {AttachmentId} uploaded for feedback {FeedbackId}",
            attachment.Id, feedbackId);

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachment);
    }

    /// <summary>
    /// Upload a video attachment for feedback.
    /// </summary>
    /// <param name="feedbackId">Feedback ID</param>
    /// <param name="file">Video file</param>
    [HttpPost("{feedbackId}/attachments/video")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<FeedbackAttachment>> UploadVideo(long feedbackId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest($"File size exceeds maximum of {MaxFileSize / (1024 * 1024)}MB");
        }

        if (!AllowedVideoTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest($"Invalid video type. Allowed types: {string.Join(", ", AllowedVideoTypes)}");
        }

        var feedback = await _context.Feedbacks.FindAsync(feedbackId);
        if (feedback == null)
        {
            return NotFound($"Feedback {feedbackId} not found");
        }

        // In production, upload to cloud storage and process video
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var storageUrl = $"/uploads/feedback/{feedbackId}/videos/{fileName}";
        var thumbnailUrl = $"/uploads/feedback/{feedbackId}/videos/thumb_{fileName}.jpg";

        var attachment = new FeedbackAttachment
        {
            FeedbackId = feedbackId,
            AttachmentType = "video",
            FileName = file.FileName,
            ContentType = file.ContentType,
            StorageUrl = storageUrl,
            ThumbnailUrl = thumbnailUrl,
            FileSizeBytes = file.Length,
            DurationSeconds = null, // Would be extracted from video metadata
            UploadedAt = DateTimeOffset.UtcNow
        };

        _context.FeedbackAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Video attachment {AttachmentId} uploaded for feedback {FeedbackId}",
            attachment.Id, feedbackId);

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachment);
    }

    /// <summary>
    /// Get feedback by ID.
    /// </summary>
    /// <param name="id">Feedback ID</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Feedback>> GetFeedback(long id)
    {
        var feedback = await _context.Feedbacks
            .Include(f => f.Business)
            .Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (feedback == null)
        {
            return NotFound();
        }

        return Ok(feedback);
    }

    /// <summary>
    /// Get attachment by ID.
    /// </summary>
    /// <param name="id">Attachment ID</param>
    [HttpGet("attachments/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackAttachment>> GetAttachment(long id)
    {
        var attachment = await _context.FeedbackAttachments.FindAsync(id);

        if (attachment == null)
        {
            return NotFound();
        }

        return Ok(attachment);
    }

    /// <summary>
    /// Get all feedback for a business.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="includeAttachments">Include attachment details</param>
    /// <param name="publicOnly">Only return public, approved feedback</param>
    [HttpGet("business/{businessId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Feedback>>> GetBusinessFeedback(
        long businessId,
        [FromQuery] bool includeAttachments = false,
        [FromQuery] bool publicOnly = true)
    {
        var query = _context.Feedbacks
            .Where(f => f.BusinessId == businessId);

        if (publicOnly)
        {
            query = query.Where(f => f.IsPublic && f.IsApproved);
        }

        if (includeAttachments)
        {
            query = query.Include(f => f.Attachments);
        }

        var feedback = await query
            .OrderByDescending(f => f.SubmittedAt)
            .Take(100)
            .ToListAsync();

        return Ok(feedback);
    }

    /// <summary>
    /// Get feedback statistics for a business.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    [HttpGet("business/{businessId}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetFeedbackStats(long businessId)
    {
        var feedback = await _context.Feedbacks
            .Where(f => f.BusinessId == businessId && f.IsPublic && f.IsApproved)
            .ToListAsync();

        var stats = new
        {
            TotalCount = feedback.Count,
            AverageRating = feedback.Where(f => f.Rating.HasValue).Average(f => f.Rating),
            RatingDistribution = feedback
                .Where(f => f.Rating.HasValue)
                .GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key!.Value, g => g.Count()),
            TypeDistribution = feedback
                .GroupBy(f => f.FeedbackType)
                .ToDictionary(g => g.Key, g => g.Count()),
            RecentCount = feedback.Count(f => f.SubmittedAt >= DateTimeOffset.UtcNow.AddDays(-30))
        };

        return Ok(stats);
    }
}
