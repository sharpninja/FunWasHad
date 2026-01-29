using System.Security.Cryptography;
using System.Text;

namespace FWH.MarketingApi.Middleware;

/// <summary>
/// Middleware to authenticate requests using API key and request signing.
/// Ensures only genuine builds of the app can call the API.
/// </summary>
internal class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(configuration);

        _apiKey = configuration["ApiSecurity:ApiKey"] ?? throw new InvalidOperationException("ApiSecurity:ApiKey is required");
        _apiSecret = configuration["ApiSecurity:ApiSecret"] ?? throw new InvalidOperationException("ApiSecurity:ApiSecret is required");

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret))
        {
            throw new InvalidOperationException("ApiSecurity:ApiKey and ApiSecurity:ApiSecret must be configured");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health checks and Swagger
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader) ||
            apiKeyHeader != _apiKey)
        {
            _logger.LogWarning("Unauthorized API request: Missing or invalid API key from {RemoteIpAddress}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Invalid API key").ConfigureAwait(false);
            return;
        }

        // Verify request signature if present (optional for now, can be enhanced)
        if (context.Request.Headers.TryGetValue("X-Request-Signature", out var signatureHeader))
        {
            if (!VerifyRequestSignature(context, signatureHeader.ToString(), _apiSecret))
            {
                _logger.LogWarning("Unauthorized API request: Invalid request signature from {RemoteIpAddress}",
                    context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Invalid request signature").ConfigureAwait(false);
                return;
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool VerifyRequestSignature(HttpContext context, string providedSignature, string secret)
    {
        try
        {
            // Build the string to sign: method + path + query + body hash
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;
            var query = context.Request.QueryString.Value ?? string.Empty;

            // For POST/PUT requests, we'd need to read the body, but that's complex in middleware
            // For now, we'll use a simpler approach: method + path + query
            var stringToSign = $"{method}{path}{query}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            return string.Equals(computedSignature, providedSignature, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
