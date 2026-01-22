using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;

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
