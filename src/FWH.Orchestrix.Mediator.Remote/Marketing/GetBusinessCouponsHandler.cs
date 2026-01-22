using System.Net.Http.Json;
using FWH.Orchestrix.Contracts.Marketing;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Orchestrix.Mediator.Remote.Marketing;

/// <summary>
/// Remote handler for getting business coupons via HTTP API.
/// </summary>
public class GetBusinessCouponsHandler : IMediatorHandler<GetBusinessCouponsRequest, GetBusinessCouponsResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetBusinessCouponsHandler> _logger;

    public GetBusinessCouponsHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetBusinessCouponsHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketingApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetBusinessCouponsResponse> HandleAsync(
        GetBusinessCouponsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting business coupons remotely for business {BusinessId}",
                request.BusinessId);

            var response = await _httpClient.GetAsync($"/api/marketing/{request.BusinessId}/coupons", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var coupons = await response.Content.ReadFromJsonAsync<List<CouponDto>>(cancellationToken);
                return new GetBusinessCouponsResponse
                {
                    Success = true,
                    Coupons = coupons ?? new List<CouponDto>()
                };
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to get business coupons: {StatusCode} - {Error}",
                response.StatusCode, error);

            return new GetBusinessCouponsResponse
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}: {error}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business coupons remotely");
            return new GetBusinessCouponsResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
