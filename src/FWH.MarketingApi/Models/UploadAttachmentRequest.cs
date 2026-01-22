namespace FWH.MarketingApi.Models;

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
