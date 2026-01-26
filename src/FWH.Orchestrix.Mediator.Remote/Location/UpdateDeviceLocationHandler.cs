using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Location;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Location;

/// <summary>
/// Remote handler for updating device location via HTTP API.
///
/// ⚠️ WARNING: This handler should NOT be used in the mobile app.
/// TR-MOBILE-001: Device location is tracked in the local SQLite database only.
/// Device location should NEVER be sent to the API for privacy and performance reasons.
///
/// This handler exists for potential future server-to-server scenarios only.
/// The mobile app uses LocationTrackingService with NotesDbContext for local storage.
/// </summary>
public class UpdateDeviceLocationHandler : IMediatorHandler<UpdateDeviceLocationRequest, UpdateDeviceLocationResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateDeviceLocationHandler> _logger;

    public UpdateDeviceLocationHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<UpdateDeviceLocationHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("LocationApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateDeviceLocationResponse> HandleAsync(
        UpdateDeviceLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
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
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LocationCreatedDto>(cancellationToken).ConfigureAwait(false);
                return new UpdateDeviceLocationResponse
                {
                    Success = true,
                    LocationId = result?.Id
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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
