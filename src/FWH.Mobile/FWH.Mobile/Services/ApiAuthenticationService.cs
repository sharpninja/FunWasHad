using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Service for adding API authentication headers to HTTP requests.
/// Ensures only genuine builds of the app can call the API.
/// </summary>
public interface IApiAuthenticationService
{
    /// <summary>
    /// Adds authentication headers to the HTTP request.
    /// </summary>
    void AddAuthenticationHeaders(HttpRequestMessage request, string? requestBody = null);
}

/// <summary>
/// Implementation of API authentication service using API key and request signing.
/// </summary>
public class ApiAuthenticationService : IApiAuthenticationService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly ILogger<ApiAuthenticationService>? _logger;

    public ApiAuthenticationService(
        string apiKey,
        string apiSecret,
        ILogger<ApiAuthenticationService>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        if (string.IsNullOrWhiteSpace(apiSecret))
            throw new ArgumentException("API secret cannot be null or empty", nameof(apiSecret));

        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _logger = logger;
    }

    public void AddAuthenticationHeaders(HttpRequestMessage request, string? requestBody = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Add API key header
        request.Headers.Add("X-API-Key", _apiKey);

        // Generate and add request signature
        var signature = GenerateRequestSignature(request, requestBody);
        if (!string.IsNullOrEmpty(signature))
        {
            request.Headers.Add("X-Request-Signature", signature);
        }
    }

    private string GenerateRequestSignature(HttpRequestMessage request, string? requestBody)
    {
        try
        {
            // Build the string to sign: method + path + query + body hash
            var method = request.Method.Method;
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;

            // Include body hash if present
            var bodyHash = string.Empty;
            if (!string.IsNullOrEmpty(requestBody))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                var hashBytes = SHA256.HashData(bodyBytes);
                bodyHash = Convert.ToBase64String(hashBytes);
            }

            var stringToSign = $"{method}{path}{query}{bodyHash}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            return signature;
        }
        catch (CryptographicException ex)
        {
            _logger?.LogWarning(ex, "Failed to generate request signature due to cryptographic error");
            return string.Empty;
        }
        catch (ArgumentException ex)
        {
            _logger?.LogWarning(ex, "Failed to generate request signature due to invalid argument");
            return string.Empty;
        }
    }
}
