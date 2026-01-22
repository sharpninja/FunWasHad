using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

/// <summary>
/// Remote handler for uploading feedback attachments via HTTP API.
/// </summary>
public class UploadFeedbackAttachmentHandler : IMediatorHandler<UploadFeedbackAttachmentRequest, UploadFeedbackAttachmentResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadFeedbackAttachmentHandler> _logger;

    public UploadFeedbackAttachmentHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<UploadFeedbackAttachmentHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UploadFeedbackAttachmentResponse> HandleAsync(
        UploadFeedbackAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading {AttachmentType} attachment remotely for feedback {FeedbackId}",
                request.AttachmentType, request.FeedbackId);

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(request.FileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
            content.Add(fileContent, "file", request.FileName);

            var endpoint = request.AttachmentType.ToLower() == "image"
                ? $"/api/feedback/{request.FeedbackId}/attachments/image"
                : $"/api/feedback/{request.FeedbackId}/attachments/video";

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AttachmentCreatedDto>(cancellationToken);
                return new UploadFeedbackAttachmentResponse
                {
                    Success = true,
                    AttachmentId = result?.Id,
                    StorageUrl = result?.StorageUrl
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to upload feedback attachment: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new UploadFeedbackAttachmentResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading feedback attachment remotely");
            return new UploadFeedbackAttachmentResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private record AttachmentCreatedDto(long Id, string StorageUrl);
}
