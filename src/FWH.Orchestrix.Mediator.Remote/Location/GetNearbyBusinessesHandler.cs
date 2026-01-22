using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Location;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Orchestrix.Mediator.Remote.Location;

/// <summary>
/// Remote handler for getting nearby businesses via HTTP API.
/// </summary>
public class GetNearbyBusinessesHandler : IMediatorHandler<GetNearbyBusinessesRequest, GetNearbyBusinessesResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetNearbyBusinessesHandler> _logger;

    public GetNearbyBusinessesHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetNearbyBusinessesHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LocationApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetNearbyBusinessesResponse> HandleAsync(
        GetNearbyBusinessesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting nearby businesses remotely at ({Lat}, {Lon})",
                request.Latitude, request.Longitude);

            var query = $"?latitude={request.Latitude}&longitude={request.Longitude}&radius={request.RadiusMeters}";
            if (request.Tags != null && request.Tags.Length > 0)
            {
                query += $"&tags={string.Join(",", request.Tags.Select(Uri.EscapeDataString))}";
            }

            var response = await _httpClient.GetAsync($"/api/locations/nearby{query}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var businesses = await response.Content.ReadFromJsonAsync<List<BusinessDto>>(cancellationToken);
                return new GetNearbyBusinessesResponse
                {
                    Success = true,
                    Businesses = businesses ?? new List<BusinessDto>(),
                    TotalCount = businesses?.Count ?? 0
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to get nearby businesses: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new GetNearbyBusinessesResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nearby businesses remotely");
            return new GetNearbyBusinessesResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
