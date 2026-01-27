using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Location;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Location;

public partial class UpdateDeviceLocationHandler : IMediatorHandler<UpdateDeviceLocationRequest, UpdateDeviceLocationResponse>
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
            Log.UpdatingDeviceLocation(_logger, request.DeviceId);

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
            Log.UpdateDeviceLocationFailed(_logger, response.StatusCode, error);

            return new UpdateDeviceLocationResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.UpdateDeviceLocationHttpError(_logger, ex);
            return new UpdateDeviceLocationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Log.UpdateDeviceLocationCanceled(_logger, ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Log.UpdateDeviceLocationTimeout(_logger, ex);
            return new UpdateDeviceLocationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private record LocationCreatedDto(long Id);

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Updating device location remotely for device {DeviceId}")]
        public static partial void UpdatingDeviceLocation(ILogger logger, string deviceId);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to update device location: {StatusCode} - {Error}")]
        public static partial void UpdateDeviceLocationFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string error);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "HTTP error updating device location remotely")]
        public static partial void UpdateDeviceLocationHttpError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Update device location canceled")]
        public static partial void UpdateDeviceLocationCanceled(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Update device location timed out")]
        public static partial void UpdateDeviceLocationTimeout(ILogger logger, Exception exception);
    }
}
