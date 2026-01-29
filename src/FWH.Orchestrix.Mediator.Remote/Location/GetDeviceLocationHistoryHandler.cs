using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Location;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Location;

public partial class GetDeviceLocationHistoryHandler : IMediatorHandler<GetDeviceLocationHistoryRequest, GetDeviceLocationHistoryResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetDeviceLocationHistoryHandler> _logger;

    public GetDeviceLocationHistoryHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetDeviceLocationHistoryHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("LocationApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetDeviceLocationHistoryResponse> HandleAsync(
        GetDeviceLocationHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            Log.GettingDeviceLocationHistory(_logger, request.DeviceId);

            var query = $"?deviceId={Uri.EscapeDataString(request.DeviceId)}";
            if (request.Since.HasValue)
            {
                query += $"&since={Uri.EscapeDataString(request.Since.Value.ToString("O"))}";
            }

            if (request.Until.HasValue)
            {
                query += $"&until={Uri.EscapeDataString(request.Until.Value.ToString("O"))}";
            }

            if (request.Limit.HasValue)
            {
                query += $"&limit={request.Limit}";
            }

            var response = await _httpClient.GetAsync(new Uri($"/api/locations{query}", UriKind.Relative), cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var locations = await response.Content.ReadFromJsonAsync<List<DeviceLocationDto>>(cancellationToken).ConfigureAwait(false);
                return new GetDeviceLocationHistoryResponse
                {
                    Success = true,
                    Locations = locations ?? new List<DeviceLocationDto>()
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Log.GetDeviceLocationHistoryFailed(_logger, response.StatusCode, error);

            return new GetDeviceLocationHistoryResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.GetDeviceLocationHistoryHttpError(_logger, ex);
            return new GetDeviceLocationHistoryResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Log.GetDeviceLocationHistoryCanceled(_logger, ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Log.GetDeviceLocationHistoryTimeout(_logger, ex);
            return new GetDeviceLocationHistoryResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 30, Level = LogLevel.Information, Message = "Getting device location history remotely for device {DeviceId}")]
        public static partial void GettingDeviceLocationHistory(ILogger logger, string deviceId);

        [LoggerMessage(EventId = 31, Level = LogLevel.Warning, Message = "Failed to get device location history: {StatusCode} - {Error}")]
        public static partial void GetDeviceLocationHistoryFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string error);

        [LoggerMessage(EventId = 32, Level = LogLevel.Error, Message = "HTTP error getting device location history remotely")]
        public static partial void GetDeviceLocationHistoryHttpError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 33, Level = LogLevel.Error, Message = "Get device location history canceled")]
        public static partial void GetDeviceLocationHistoryCanceled(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 34, Level = LogLevel.Error, Message = "Get device location history timed out")]
        public static partial void GetDeviceLocationHistoryTimeout(ILogger logger, Exception exception);
    }
}
