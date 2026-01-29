using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

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
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UploadFeedbackAttachmentResponse> HandleAsync(
        UploadFeedbackAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            _logger.LogInformation("Uploading {AttachmentType} attachment remotely for feedback {FeedbackId}",
                request.AttachmentType, request.FeedbackId);

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(request.FileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
            content.Add(fileContent, "file", request.FileName);

            var endpoint = string.Equals(request.AttachmentType, "image", StringComparison.OrdinalIgnoreCase)
                ? $"/api/feedback/{request.FeedbackId}/attachments/image"
                : $"/api/feedback/{request.FeedbackId}/attachments/video";

            var response = await _httpClient.PostAsync(new Uri(endpoint, UriKind.Relative), content, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AttachmentCreatedDto>(cancellationToken).ConfigureAwait(false);
                return new UploadFeedbackAttachmentResponse
                {
                    Success = true,
                    AttachmentId = result?.Id,
                    StorageUrl = result?.StorageUrl
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
