using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;

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
