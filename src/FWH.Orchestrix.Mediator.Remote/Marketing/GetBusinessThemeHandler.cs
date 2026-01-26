using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

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
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessThemeResponse> HandleAsync(
        GetBusinessThemeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            _logger.LogInformation("Getting business theme remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.GetAsync(new Uri($"/api/marketing/{request.BusinessId}/theme", UriKind.Relative), cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var theme = await response.Content.ReadFromJsonAsync<BusinessThemeDto>(cancellationToken).ConfigureAwait(false);
                return new GetBusinessThemeResponse
                {
                    Success = true,
                    Theme = theme
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
