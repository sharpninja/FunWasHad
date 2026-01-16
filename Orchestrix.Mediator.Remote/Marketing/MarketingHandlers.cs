using System.Net.Http.Json;
using Orchestrix.Contracts.Marketing;
using Microsoft.Extensions.Logging;
using Orchestrix.Contracts.Mediator;

namespace Orchestrix.Mediator.Remote.Marketing;

/// <summary>
/// Remote handler for getting business marketing data via HTTP API.
/// </summary>
public class GetBusinessMarketingHandler : IMediatorHandler<GetBusinessMarketingRequest, GetBusinessMarketingResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetBusinessMarketingHandler> _logger;

    public GetBusinessMarketingHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetBusinessMarketingHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessMarketingResponse> HandleAsync(
        GetBusinessMarketingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting business marketing data remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.GetAsync($"/api/marketing/{request.BusinessId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<BusinessMarketingDto>(cancellationToken);
                return new GetBusinessMarketingResponse
                {
                    Success = true,
                    Data = data
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to get business marketing data: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new GetBusinessMarketingResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business marketing data remotely");
            return new GetBusinessMarketingResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Remote handler for getting business theme via HTTP API.
/// </summary>
public class GetBusinessThemeHandler : IMediatorHandler<GetBusinessThemeRequest, GetBusinessThemeResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetBusinessThemeHandler> _logger;

    public GetBusinessThemeHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetBusinessThemeHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessThemeResponse> HandleAsync(
        GetBusinessThemeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting business theme remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.GetAsync($"/api/marketing/{request.BusinessId}/theme", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var theme = await response.Content.ReadFromJsonAsync<BusinessThemeDto>(cancellationToken);
                return new GetBusinessThemeResponse
                {
                    Success = true,
                    Theme = theme
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to get business theme: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new GetBusinessThemeResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business theme remotely");
            return new GetBusinessThemeResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Remote handler for getting business coupons via HTTP API.
/// </summary>
public class GetBusinessCouponsHandler : IMediatorHandler<GetBusinessCouponsRequest, GetBusinessCouponsResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetBusinessCouponsHandler> _logger;

    public GetBusinessCouponsHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetBusinessCouponsHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessCouponsResponse> HandleAsync(
        GetBusinessCouponsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting business coupons remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.GetAsync($"/api/marketing/{request.BusinessId}/coupons", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var coupons = await response.Content.ReadFromJsonAsync<List<CouponDto>>(cancellationToken);
                return new GetBusinessCouponsResponse
                {
                    Success = true,
                    Coupons = coupons ?? new List<CouponDto>()
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to get business coupons: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new GetBusinessCouponsResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business coupons remotely");
            return new GetBusinessCouponsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Remote handler for submitting feedback via HTTP API.
/// </summary>
public class SubmitFeedbackHandler : IMediatorHandler<SubmitFeedbackRequest, SubmitFeedbackResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubmitFeedbackHandler> _logger;

    public SubmitFeedbackHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<SubmitFeedbackHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SubmitFeedbackResponse> HandleAsync(
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting feedback remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/feedback",
                new
                {
                    request.BusinessId,
                    request.UserId,
                    request.UserName,
                    request.UserEmail,
                    request.FeedbackType,
                    request.Subject,
                    request.Message,
                    request.Rating,
                    request.IsPublic,
                    request.Latitude,
                    request.Longitude
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FeedbackCreatedDto>(cancellationToken);
                return new SubmitFeedbackResponse
                {
                    Success = true,
                    FeedbackId = result?.Id
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to submit feedback: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new SubmitFeedbackResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback remotely");
            return new SubmitFeedbackResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private record FeedbackCreatedDto(long Id);
}

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
