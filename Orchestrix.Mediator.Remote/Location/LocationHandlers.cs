using System.Net.Http.Json;
using Orchestrix.Contracts.Location;
using Microsoft.Extensions.Logging;
using Orchestrix.Contracts.Mediator;

namespace Orchestrix.Mediator.Remote.Location;

/// <summary>
/// Remote handler for updating device location via HTTP API.
/// </summary>
public class UpdateDeviceLocationHandler : IMediatorHandler<UpdateDeviceLocationRequest, UpdateDeviceLocationResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateDeviceLocationHandler> _logger;

    public UpdateDeviceLocationHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<UpdateDeviceLocationHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LocationApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateDeviceLocationResponse> HandleAsync(
        UpdateDeviceLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating device location remotely for device {DeviceId}",
                request.DeviceId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/locations",
                new
                {
                    request.DeviceId,
                    request.Latitude,
                    request.Longitude,
                    request.Accuracy,
                    request.Altitude,
                    request.Speed,
                    request.Heading,
                    request.Timestamp
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LocationCreatedDto>(cancellationToken);
                return new UpdateDeviceLocationResponse
                {
                    Success = true,
                    LocationId = result?.Id
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to update device location: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new UpdateDeviceLocationResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device location remotely");
            return new UpdateDeviceLocationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private record LocationCreatedDto(long Id);
}

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
