using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

public partial class GetBusinessMarketingHandler : IMediatorHandler<GetBusinessMarketingRequest, GetBusinessMarketingResponse>
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
            Log.GettingBusinessMarketing(_logger, request.BusinessId);

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
            Log.GetBusinessMarketingFailed(_logger, response.StatusCode, error);

            return new GetBusinessMarketingResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.GetBusinessMarketingHttpError(_logger, ex);
            return new GetBusinessMarketingResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Log.GetBusinessMarketingCanceled(_logger, ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Log.GetBusinessMarketingTimeout(_logger, ex);
            return new GetBusinessMarketingResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Getting business marketing data remotely for business {BusinessId}")]
        public static partial void GettingBusinessMarketing(ILogger logger, long businessId);

        [LoggerMessage(EventId = 21, Level = LogLevel.Warning, Message = "Failed to get business marketing data: {StatusCode} - {Error}")]
        public static partial void GetBusinessMarketingFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string error);

        [LoggerMessage(EventId = 22, Level = LogLevel.Error, Message = "HTTP error getting business marketing data remotely")]
        public static partial void GetBusinessMarketingHttpError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Get business marketing canceled")]
        public static partial void GetBusinessMarketingCanceled(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 24, Level = LogLevel.Error, Message = "Get business marketing timed out")]
        public static partial void GetBusinessMarketingTimeout(ILogger logger, Exception exception);
    }
}
