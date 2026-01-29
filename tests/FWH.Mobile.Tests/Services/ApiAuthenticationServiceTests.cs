using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Unit tests for ApiAuthenticationService.
/// Tests the service that adds authentication headers to HTTP requests from the mobile app.
/// </summary>
public class ApiAuthenticationServiceTests
{
    private readonly ILogger<ApiAuthenticationService> _logger;

    public ApiAuthenticationServiceTests()
    {
        _logger = Substitute.For<ILogger<ApiAuthenticationService>>();
    }

    /// <summary>
    /// Tests that AddAuthenticationHeaders adds X-API-Key header with the configured API key.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to add the API key header to HTTP requests.</para>
    /// <para><strong>Data involved:</strong> API key "test-api-key" and API secret "test-api-secret" configured in the service. A GET request to /api/marketing/nearby with no existing headers.</para>
    /// <para><strong>Why the data matters:</strong> The API key header is required for authentication. This test ensures the service correctly adds this header so requests can be authenticated by the API.</para>
    /// <para><strong>Expected outcome:</strong> The request contains X-API-Key header with value "test-api-key".</para>
    /// <para><strong>Reason for expectation:</strong> The service should automatically add the configured API key to all requests, enabling seamless authentication without requiring manual header management in each HTTP call.</para>
    /// </remarks>
    [Fact]
    public void AddAuthenticationHeadersAddsApiKeyHeader()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        var service = new ApiAuthenticationService(apiKey, apiSecret, _logger);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/marketing/nearby");

        // Act
        service.AddAuthenticationHeaders(request);

        // Assert
        Assert.True(request.Headers.Contains("X-API-Key"));
        var headerValues = request.Headers.GetValues("X-API-Key");
        Assert.Contains(apiKey, headerValues);
    }

    /// <summary>
    /// Tests that AddAuthenticationHeaders adds X-Request-Signature header with computed HMAC signature.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's ability to generate and add request signatures for additional security.</para>
    /// <para><strong>Data involved:</strong> API key "test-api-key" and API secret "test-api-secret". A GET request to /api/marketing/nearby?latitude=40.7128&longitude=-74.0060. The signature should be computed from method, path, query, and body hash.</para>
    /// <para><strong>Why the data matters:</strong> Request signatures provide tamper-proof authentication. This test ensures signatures are correctly computed and added, enabling the API to verify request authenticity.</para>
    /// <para><strong>Expected outcome:</strong> The request contains X-Request-Signature header with a valid HMAC-SHA256 signature.</para>
    /// <para><strong>Reason for expectation:</strong> The service should compute signatures using HMAC-SHA256 with the API secret, ensuring requests cannot be tampered with without knowledge of the secret.</para>
    /// </remarks>
    [Fact]
    public void AddAuthenticationHeadersAddsRequestSignature()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        var service = new ApiAuthenticationService(apiKey, apiSecret, _logger);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/marketing/nearby?latitude=40.7128&longitude=-74.0060");

        // Act
        service.AddAuthenticationHeaders(request);

        // Assert
        Assert.True(request.Headers.Contains("X-Request-Signature"));
        var signatureHeader = request.Headers.GetValues("X-Request-Signature").FirstOrDefault();
        Assert.NotNull(signatureHeader);
        Assert.NotEmpty(signatureHeader);
    }

    /// <summary>
    /// Tests that request signature matches expected HMAC-SHA256 computation.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The correctness of the signature computation algorithm.</para>
    /// <para><strong>Data involved:</strong> A GET request to /api/marketing/nearby?latitude=40.7128&longitude=-74.0060 with no body. The signature should be computed as HMAC-SHA256(method + path + query + bodyHash, secret).</para>
    /// <para><strong>Why the data matters:</strong> Signature computation must match exactly between client and server. Incorrect computation would cause all authenticated requests to fail, breaking the app.</para>
    /// <para><strong>Expected outcome:</strong> The signature in the header matches the manually computed signature using the same algorithm.</para>
    /// <para><strong>Reason for expectation:</strong> The signature algorithm must be deterministic and match the server-side computation. This test verifies the implementation correctness by comparing against a manually computed signature.</para>
    /// </remarks>
    [Fact]
    public void AddAuthenticationHeadersSignatureMatchesExpectedComputation()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        var service = new ApiAuthenticationService(apiKey, apiSecret, _logger);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/marketing/nearby?latitude=40.7128&longitude=-74.0060");

        // Compute expected signature manually
        var method = "GET";
        var path = "/api/marketing/nearby";
        var query = "?latitude=40.7128&longitude=-74.0060";
        var bodyHash = string.Empty; // No body for GET request
        var stringToSign = $"{method}{path}{query}{bodyHash}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var expectedSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        // Act
        service.AddAuthenticationHeaders(request);

        // Assert
        var actualSignature = request.Headers.GetValues("X-Request-Signature").FirstOrDefault();
        Assert.Equal(expectedSignature, actualSignature);
    }

    /// <summary>
    /// Tests that request signature includes body hash for POST requests with body.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's inclusion of request body in signature computation for POST requests.</para>
    /// <para><strong>Data involved:</strong> A POST request to /api/feedback with JSON body '{"businessId":1,"message":"test"}'. The signature should include the SHA256 hash of the body content.</para>
    /// <para><strong>Why the data matters:</strong> Including the body hash in signatures prevents request tampering. If the body is modified, the signature will be invalid, protecting against man-in-the-middle attacks.</para>
    /// <para><strong>Expected outcome:</strong> The signature includes the body hash in its computation, and changing the body produces a different signature.</para>
    /// <para><strong>Reason for expectation:</strong> POST requests with bodies must include body content in signature computation to ensure request integrity. The signature should change if the body changes, preventing tampering.</para>
    /// </remarks>
    [Fact]
    public void AddAuthenticationHeaders_IncludesBodyHashInSignature()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        var service = new ApiAuthenticationService(apiKey, apiSecret, _logger);
        var requestBody = "{\"businessId\":1,\"message\":\"test\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/feedback")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        // Act
        service.AddAuthenticationHeaders(request, requestBody);

        // Assert
        var signature = request.Headers.GetValues("X-Request-Signature").FirstOrDefault();
        Assert.NotNull(signature);

        // Verify signature changes when body changes
        var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/feedback")
        {
            Content = new StringContent("{\"businessId\":2,\"message\":\"different\"}", Encoding.UTF8, "application/json")
        };
        service.AddAuthenticationHeaders(request2, "{\"businessId\":2,\"message\":\"different\"}");

        var signature2 = request2.Headers.GetValues("X-Request-Signature").FirstOrDefault();
        Assert.NotEqual(signature, signature2);
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when API key is null or empty.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's validation of required API key parameter during construction.</para>
    /// <para><strong>Data involved:</strong> null or empty string for apiKey parameter. The service requires a valid API key to function.</para>
    /// <para><strong>Why the data matters:</strong> Missing API key would cause all requests to fail authentication. This test ensures the service fails fast with a clear error message rather than failing silently at runtime.</para>
    /// <para><strong>Expected outcome:</strong> ArgumentException is thrown with a message indicating API key cannot be null or empty.</para>
    /// <para><strong>Reason for expectation:</strong> The service cannot function without an API key. It should validate this requirement at construction time, providing immediate feedback about configuration errors.</para>
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidApiKey_ThrowsArgumentException(string? apiKey)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiAuthenticationService(apiKey!, "test-secret", _logger));

        Assert.Contains("API key", ex.Message);
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when API secret is null or empty.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The service's validation of required API secret parameter during construction.</para>
    /// <para><strong>Data involved:</strong> null or empty string for apiSecret parameter. The service requires a valid API secret for signature computation.</para>
    /// <para><strong>Why the data matters:</strong> Missing API secret would prevent signature generation, causing signature validation to fail. This test ensures the service validates this requirement at construction.</para>
    /// <para><strong>Expected outcome:</strong> ArgumentException is thrown with a message indicating API secret cannot be null or empty.</para>
    /// <para><strong>Reason for expectation:</strong> The service requires both API key and secret to function properly. Missing secret should cause immediate failure with a clear error message.</para>
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorInvalidApiSecretThrowsArgumentException(string? apiSecret)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ApiAuthenticationService("test-key", apiSecret!, _logger));

        Assert.Contains("API secret", ex.Message);
    }
}
