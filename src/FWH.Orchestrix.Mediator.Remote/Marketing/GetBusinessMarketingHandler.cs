using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

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
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessMarketingResponse> HandleAsync(
        GetBusinessMarketingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            _logger.LogInformation("Getting business marketing data remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.GetAsync(new Uri($"/api/marketing/{request.BusinessId}", UriKind.Relative), cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<BusinessMarketingDto>(cancellationToken).ConfigureAwait(false);
                return new GetBusinessMarketingResponse
                {
                    Success = true,
                    Data = data
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
