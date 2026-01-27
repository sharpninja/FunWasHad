using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.Logging;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

public partial class GetBusinessThemeHandler : IMediatorHandler<GetBusinessThemeRequest, GetBusinessThemeResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetBusinessThemeHandler> _logger;

    public GetBusinessThemeHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetBusinessThemeHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessThemeResponse> HandleAsync(
        GetBusinessThemeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            Log.GettingBusinessTheme(_logger, request.BusinessId);

            var response = await _httpClient.GetAsync(new Uri($"/api/marketing/{request.BusinessId}/theme", UriKind.Relative), cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var theme = await response.Content.ReadFromJsonAsync<BusinessThemeDto>(cancellationToken).ConfigureAwait(false);
                return new GetBusinessThemeResponse
                {
                    Success = true,
                    Theme = theme
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Log.GetBusinessThemeFailed(_logger, response.StatusCode, error);

            return new GetBusinessThemeResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (HttpRequestException ex)
        {
            Log.GetBusinessThemeHttpError(_logger, ex);
            return new GetBusinessThemeResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Log.GetBusinessThemeCanceled(_logger, ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Log.GetBusinessThemeTimeout(_logger, ex);
            return new GetBusinessThemeResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Getting business theme remotely for business {BusinessId}")]
        public static partial void GettingBusinessTheme(ILogger logger, long businessId);

        [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Failed to get business theme: {StatusCode} - {Error}")]
        public static partial void GetBusinessThemeFailed(ILogger logger, System.Net.HttpStatusCode statusCode, string error);

        [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "HTTP error getting business theme remotely")]
        public static partial void GetBusinessThemeHttpError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Get business theme canceled")]
        public static partial void GetBusinessThemeCanceled(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Get business theme timed out")]
        public static partial void GetBusinessThemeTimeout(ILogger logger, Exception exception);
    }
}
