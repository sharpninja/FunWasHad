using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Location;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Orchestrix.Mediator.Remote.Location;

/// <summary>
/// Remote handler for getting device location history via HTTP API.
/// </summary>
public class GetDeviceLocationHistoryHandler : IMediatorHandler<GetDeviceLocationHistoryRequest, GetDeviceLocationHistoryResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetDeviceLocationHistoryHandler> _logger;

    public GetDeviceLocationHistoryHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetDeviceLocationHistoryHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LocationApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetDeviceLocationHistoryResponse> HandleAsync(
        GetDeviceLocationHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting device location history remotely for device {DeviceId}",
                request.DeviceId);

            var query = $"?deviceId={Uri.EscapeDataString(request.DeviceId)}";
            if (request.Since.HasValue)
                query += $"&since={Uri.EscapeDataString(request.Since.Value.ToString("O"))}";
            if (request.Until.HasValue)
                query += $"&until={Uri.EscapeDataString(request.Until.Value.ToString("O"))}";
            if (request.Limit.HasValue)
                query += $"&limit={request.Limit}";

            var response = await _httpClient.GetAsync($"/api/locations{query}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var locations = await response.Content.ReadFromJsonAsync<List<DeviceLocationDto>>(cancellationToken);
                return new GetDeviceLocationHistoryResponse
                {
                    Success = true,
                    Locations = locations ?? new List<DeviceLocationDto>()
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to get device location history: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new GetDeviceLocationHistoryResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device location history remotely");
            return new GetDeviceLocationHistoryResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
