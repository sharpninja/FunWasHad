using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FWH.MarketingApi.Controllers;

/// <summary>
/// API controller for submitting and managing user feedback to businesses.
/// Implements TR-API-003: Feedback endpoints.
/// </summary>
/// <remarks>
/// This controller provides all feedback-related endpoints as specified in TR-API-003:
/// - Feedback submission (TR-API-003)
/// - Image and video attachment uploads (TR-MEDIA-001, TR-MEDIA-002)
/// - Feedback retrieval and statistics
/// </remarks>
[ApiController]
[Route("api/[controller]")]
internal partial class FeedbackController : ControllerBase
{
    private readonly MarketingDbContext _context;
    private readonly ILogger<FeedbackController> _logger;
    private readonly FWH.MarketingApi.Services.IBlobStorageService _blobStorage;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private static readonly string[] AllowedVideoTypes = { "video/mp4", "video/quicktime", "video/x-msvideo" };

    public FeedbackController(
        MarketingDbContext context,
        ILogger<FeedbackController> logger,
        FWH.MarketingApi.Services.IBlobStorageService blobStorage)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    }

    /// <summary>
    /// Submit text feedback to a business.
    /// Implements TR-API-003: POST /api/feedback.
    /// </summary>
    /// <param name="request">Feedback details including business ID, user info, feedback type, and message</param>
    /// <returns>Created feedback with ID</returns>
    /// <exception cref="BadRequestResult">Thrown when request is invalid, feedback type is invalid, rating is out of range, or business is not subscribed</exception>
    /// <exception cref="NotFoundResult">Thrown when business is not found</exception>
    /// <remarks>
    /// Validates feedback according to TR-API-004 (validation requirements).
    /// Rating must be between 1 and 5 if provided.
    /// </remarks>
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
        var business = await _context.Businesses.FindAsync(request.BusinessId).ConfigureAwait(false);
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
        await _context.SaveChangesAsync().ConfigureAwait(false);

        Log.FeedbackSubmitted(_logger, feedback.Id, request.BusinessId, request.UserId);

        return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, feedback);
    }

    /// <summary>
    /// Upload an image attachment for feedback.
    /// Implements TR-API-003: POST /api/feedback/{feedbackId}/attachments/image.
    /// Implements TR-MEDIA-001: Attachment upload handling and TR-MEDIA-002: Content type support.
    /// </summary>
    /// <param name="feedbackId">Feedback ID to attach the image to</param>
    /// <param name="file">Image file (JPEG, PNG, GIF, or WebP, max 50MB)</param>
    /// <returns>Created attachment with metadata</returns>
    /// <exception cref="BadRequestResult">Thrown when file is missing, file size exceeds 50MB, or content type is not allowed</exception>
    /// <exception cref="NotFoundResult">Thrown when feedback is not found</exception>
    /// <remarks>
    /// Validates file size (max 50MB) and content type according to TR-MEDIA-002.
    /// Allowed image types: image/jpeg, image/png, image/gif, image/webp.
    /// </remarks>
    [HttpPost("{feedbackId}/attachments/image")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<FeedbackAttachment>> UploadImage(long feedbackId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            Log.FileMissing(_logger);
            return BadRequest("File is required");
        }

        if (file.Length > MaxFileSize)
        {
            Log.FileTooLarge(_logger, file.FileName);
            return BadRequest("File size exceeds limit");
        }

        if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
        {
            Log.FileTypeNotAllowed(_logger, file.ContentType);
            return BadRequest("Invalid image content type");
        }

        var feedback = await _context.Feedbacks.FindAsync(feedbackId).ConfigureAwait(false);
        if (feedback == null)
        {
            return NotFound($"Feedback {feedbackId} not found");
        }

        // Upload file to blob storage
        string storageUrl;
        string? thumbnailUrl;

        try
        {
            using var fileStream = file.OpenReadStream();
            (storageUrl, thumbnailUrl) = await _blobStorage.UploadWithThumbnailAsync(
                fileStream,
                file.FileName,
                file.ContentType,
                $"feedback/{feedbackId}/images",
                generateThumbnail: true,
                cancellationToken: default).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            Log.AttachmentUploadFailed(_logger, feedbackId, ex);
            return BadRequest(ex.Message);
        }

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
        await _context.SaveChangesAsync().ConfigureAwait(false);

        Log.AttachmentUploaded(_logger, attachment.Id, feedbackId);

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachment);
    }

    /// <summary>
    /// Upload a video attachment for feedback.
    /// Implements TR-API-003: POST /api/feedback/{feedbackId}/attachments/video.
    /// Implements TR-MEDIA-001: Attachment upload handling and TR-MEDIA-002: Content type support.
    /// </summary>
    /// <param name="feedbackId">Feedback ID to attach the video to</param>
    /// <param name="file">Video file (MP4, QuickTime, or AVI, max 50MB)</param>
    /// <returns>Created attachment with metadata</returns>
    /// <exception cref="BadRequestResult">Thrown when file is missing, file size exceeds 50MB, or content type is not allowed</exception>
    /// <exception cref="NotFoundResult">Thrown when feedback is not found</exception>
    /// <remarks>
    /// Validates file size (max 50MB) and content type according to TR-MEDIA-002.
    /// Allowed video types: video/mp4, video/quicktime, video/x-msvideo.
    /// </remarks>
    [HttpPost("{feedbackId}/attachments/video")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<FeedbackAttachment>> UploadVideo(long feedbackId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            Log.FileMissing(_logger);
            return BadRequest("File is required");
        }

        if (file.Length > MaxFileSize)
        {
            Log.FileTooLarge(_logger, file.FileName);
            return BadRequest("File size exceeds limit");
        }

        if (!AllowedVideoTypes.Contains(file.ContentType.ToLower()))
        {
            Log.FileTypeNotAllowed(_logger, file.ContentType);
            return BadRequest("Invalid video content type");
        }

        var feedback = await _context.Feedbacks.FindAsync(feedbackId).ConfigureAwait(false);
        if (feedback == null)
        {
            return NotFound($"Feedback {feedbackId} not found");
        }

        // Upload file to blob storage
        string storageUrl;
        string? thumbnailUrl;

        try
        {
            using var fileStream = file.OpenReadStream();
            (storageUrl, thumbnailUrl) = await _blobStorage.UploadWithThumbnailAsync(
                fileStream,
                file.FileName,
                file.ContentType,
                $"feedback/{feedbackId}/videos",
                generateThumbnail: false,
                cancellationToken: default).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            Log.AttachmentUploadFailed(_logger, feedbackId, ex);
            return BadRequest(ex.Message);
        }

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
        await _context.SaveChangesAsync().ConfigureAwait(false);

        Log.AttachmentUploaded(_logger, attachment.Id, feedbackId);

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachment);
    }

    /// <summary>
    /// Get feedback by ID.
    /// Implements TR-API-003: GET /api/feedback/{id}.
    /// </summary>
    /// <param name="id">Feedback ID</param>
    /// <returns>Feedback with business and attachments</returns>
    /// <exception cref="NotFoundResult">Thrown when feedback is not found</exception>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Feedback>> GetFeedback(long id)
    {
        var feedback = await _context.Feedbacks
            .Include(f => f.Business)
            .Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == id).ConfigureAwait(false);

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
        var attachment = await _context.FeedbackAttachments.FindAsync(id).ConfigureAwait(false);

        if (attachment == null)
        {
            return NotFound();
        }

        return Ok(attachment);
    }

    /// <summary>
    /// Get all feedback for a business with pagination.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="includeAttachments">Include attachment details</param>
    /// <param name="publicOnly">Only return public, approved feedback</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    [HttpGet("business/{businessId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Feedback>>> GetBusinessFeedback(
        long businessId,
        [FromQuery] bool includeAttachments = false,
        [FromQuery] bool publicOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var pagination = new FWH.MarketingApi.Models.PaginationParameters { Page = page, PageSize = pageSize };
        pagination.Validate();

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

        var totalCount = await query.CountAsync().ConfigureAwait(false);
        var feedback = await query
            .OrderByDescending(f => f.SubmittedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync().ConfigureAwait(false);

        var result = new FWH.MarketingApi.Models.PagedResult<Feedback>
        {
            Items = feedback,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };

        Log.FeedbackPageRetrieved(_logger, feedback.Count, pagination.Page, businessId);
        return Ok(result);
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
            .ToListAsync().ConfigureAwait(false);

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

    private static partial class Log
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Feedback {FeedbackId} submitted for business {BusinessId} by user {UserId}")]
        public static partial void FeedbackSubmitted(ILogger logger, long feedbackId, long businessId, string? userId);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "File is required")]
        public static partial void FileMissing(ILogger logger);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "File {FileName} exceeds maximum size")]
        public static partial void FileTooLarge(ILogger logger, string fileName);

        [LoggerMessage(EventId = 2004, Level = LogLevel.Warning, Message = "File type {ContentType} not allowed")]
        public static partial void FileTypeNotAllowed(ILogger logger, string? contentType);

        [LoggerMessage(EventId = 2005, Level = LogLevel.Warning, Message = "Attachment upload failed for feedback {FeedbackId}")]
        public static partial void AttachmentUploadFailed(ILogger logger, long feedbackId, Exception exception);

        [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Attachment {AttachmentId} uploaded for feedback {FeedbackId}")]
        public static partial void AttachmentUploaded(ILogger logger, long attachmentId, long feedbackId);

        [LoggerMessage(EventId = 2007, Level = LogLevel.Debug, Message = "Retrieved {Count} feedback items (page {Page}) for business {BusinessId}")]
        public static partial void FeedbackPageRetrieved(ILogger logger, int count, int page, long businessId);
    }
}
