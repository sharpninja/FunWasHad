using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

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
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SubmitFeedbackResponse> HandleAsync(
        SubmitFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
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
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FeedbackCreatedDto>(cancellationToken).ConfigureAwait(false);
                return new SubmitFeedbackResponse
                {
                    Success = true,
                    FeedbackId = result?.Id
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
