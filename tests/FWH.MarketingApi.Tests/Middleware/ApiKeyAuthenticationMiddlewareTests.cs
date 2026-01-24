using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using FWH.MarketingApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FWH.MarketingApi.Tests.Middleware;

/// <summary>
/// Unit tests for ApiKeyAuthenticationMiddleware.
/// Tests authentication mechanism that ensures only genuine builds of the app can call the API.
/// </summary>
public class ApiKeyAuthenticationMiddlewareTests
{
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly RequestDelegate _next;

    public ApiKeyAuthenticationMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<ApiKeyAuthenticationMiddleware>>();
        _configuration = Substitute.For<IConfiguration>();
        _next = Substitute.For<RequestDelegate>();
    }

    /// <summary>
    /// Tests that requests with valid API key are allowed through.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's ability to validate and allow requests with correct API key headers.</para>
    /// <para><strong>Data involved:</strong> A valid API key "test-api-key" configured in both middleware and request headers. The request path is /api/marketing/nearby, which is not excluded from authentication.</para>
    /// <para><strong>Why the data matters:</strong> Valid API key authentication is the core security mechanism. This test ensures legitimate app requests are not blocked, which is critical for app functionality.</para>
    /// <para><strong>Expected outcome:</strong> The next middleware in the pipeline is called (request proceeds).</para>
    /// <para><strong>Reason for expectation:</strong> When a valid API key is provided, the middleware should authenticate the request and allow it to proceed to the controller. The _next delegate should be invoked exactly once.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_ValidApiKey_AllowsRequest()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/marketing/nearby";
        context.Request.Headers["X-API-Key"] = apiKey;

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.Received(1).Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that requests without API key are rejected with 401 Unauthorized.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's ability to reject requests missing the required API key header.</para>
    /// <para><strong>Data involved:</strong> A request to /api/marketing/nearby without any X-API-Key header. The middleware is configured with apiKey="test-api-key".</para>
    /// <para><strong>Why the data matters:</strong> Missing API keys indicate unauthenticated requests that should be blocked. This test ensures the security mechanism properly rejects unauthorized access attempts.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 401 Unauthorized status code and the next middleware is not called.</para>
    /// <para><strong>Reason for expectation:</strong> Security requires that all API requests include valid authentication. Missing API keys should result in immediate rejection with 401 status, preventing unauthorized access to protected endpoints.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_MissingApiKey_Returns401()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/marketing/nearby";
        // No X-API-Key header

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that requests with invalid API key are rejected with 401 Unauthorized.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's validation of API key values against the configured key.</para>
    /// <para><strong>Data involved:</strong> A request with X-API-Key header set to "wrong-key" while the middleware is configured with apiKey="test-api-key". The mismatch tests key validation.</para>
    /// <para><strong>Why the data matters:</strong> Invalid API keys could be from compromised apps, reverse-engineered keys, or malicious requests. This test ensures only the exact configured key is accepted.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 401 Unauthorized status code and the next middleware is not called.</para>
    /// <para><strong>Reason for expectation:</strong> API key validation must be strict - any mismatch should result in rejection. This prevents unauthorized access even if an attacker knows the key format.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_InvalidApiKey_Returns401()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/marketing/nearby";
        context.Request.Headers.Add("X-API-Key", "wrong-key");

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that health check endpoints bypass authentication.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's exclusion of health check endpoints from authentication requirements.</para>
    /// <para><strong>Data involved:</strong> A request to /health endpoint without any API key header. Health checks are system endpoints that should be accessible without authentication for monitoring purposes.</para>
    /// <para><strong>Why the data matters:</strong> Health check endpoints are used by orchestration systems (Railway, Kubernetes, etc.) to verify service availability. Requiring authentication would break these monitoring systems.</para>
    /// <para><strong>Expected outcome:</strong> The request proceeds without authentication (next middleware is called).</para>
    /// <para><strong>Reason for expectation:</strong> Health checks are infrastructure endpoints that must be publicly accessible. The middleware should skip authentication for paths starting with /health to allow monitoring systems to function.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_HealthCheckEndpoint_BypassesAuthentication()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        // No API key header

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.Received(1).Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that Swagger endpoints bypass authentication.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's exclusion of Swagger UI endpoints from authentication requirements.</para>
    /// <para><strong>Data involved:</strong> A request to /swagger/index.html without any API key header. Swagger is a development/documentation tool that should be accessible without authentication in development/staging.</para>
    /// <para><strong>Why the data matters:</strong> Swagger UI is used for API documentation and testing during development. Requiring authentication would make it difficult to use Swagger for testing and documentation.</para>
    /// <para><strong>Expected outcome:</strong> The request proceeds without authentication (next middleware is called).</para>
    /// <para><strong>Reason for expectation:</strong> Swagger endpoints are development tools that should be accessible without authentication. The middleware should skip authentication for paths starting with /swagger.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_SwaggerEndpoint_BypassesAuthentication()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        // No API key header

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.Received(1).Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that requests with valid API key and valid signature are allowed through.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's ability to validate both API key and request signature when both are provided.</para>
    /// <para><strong>Data involved:</strong> A request with valid API key "test-api-key" and a correctly computed HMAC-SHA256 signature for the request path and query. The signature is computed using apiSecret="test-api-secret".</para>
    /// <para><strong>Why the data matters:</strong> Request signing provides additional security beyond API key validation. It ensures the request hasn't been tampered with and was generated by a client with the secret key.</para>
    /// <para><strong>Expected outcome:</strong> The request proceeds (next middleware is called).</para>
    /// <para><strong>Reason for expectation:</strong> When both API key and signature are valid, the request should be authenticated and allowed to proceed. This provides stronger security than API key alone.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_ValidApiKeyAndSignature_AllowsRequest()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/marketing/nearby";
        context.Request.QueryString = new QueryString("?latitude=40.7128&longitude=-74.0060");
        context.Request.Method = "GET";
        context.Request.Headers.Add("X-API-Key", apiKey);

        // Compute valid signature
        var stringToSign = $"GET/api/marketing/nearby?latitude=40.7128&longitude=-74.0060";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        context.Request.Headers.Add("X-Request-Signature", signature);

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.Received(1).Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that requests with valid API key but invalid signature are rejected.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's validation of request signatures when provided.</para>
    /// <para><strong>Data involved:</strong> A request with valid API key "test-api-key" but an incorrect signature "invalid-signature". The signature doesn't match the computed signature for the request.</para>
    /// <para><strong>Why the data matters:</strong> Invalid signatures could indicate tampered requests, compromised keys, or incorrect signature computation. This test ensures signature validation is enforced when signatures are provided.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 401 Unauthorized status code and the next middleware is not called.</para>
    /// <para><strong>Reason for expectation:</strong> When a signature is provided, it must be valid. Invalid signatures should result in rejection to prevent tampered or malicious requests from being processed.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsync_ValidApiKeyButInvalidSignature_Returns401()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/marketing/nearby";
        context.Request.Method = "GET";
        context.Request.Headers["X-API-Key"] = apiKey;
        context.Request.Headers["X-Request-Signature"] = "invalid-signature";

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that middleware throws InvalidOperationException when API key is not configured.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's validation of required configuration during construction.</para>
    /// <para><strong>Data involved:</strong> Configuration that doesn't provide ApiSecurity:ApiKey value. The middleware requires this configuration to function.</para>
    /// <para><strong>Why the data matters:</strong> Missing configuration would cause the middleware to fail at runtime. This test ensures configuration is validated at startup, providing clear error messages.</para>
    /// <para><strong>Expected outcome:</strong> InvalidOperationException is thrown during middleware construction.</para>
    /// <para><strong>Reason for expectation:</strong> The middleware cannot function without an API key. It should fail fast during construction rather than at runtime, making configuration errors immediately apparent.</para>
    /// </remarks>
    [Fact]
    public void Constructor_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration["ApiSecurity:ApiKey"].Returns((string?)null);
        _configuration["ApiSecurity:ApiSecret"].Returns("test-secret");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger));
        
        Assert.Contains("ApiSecurity:ApiKey is required", ex.Message);
    }

    /// <summary>
    /// Tests that middleware throws InvalidOperationException when API secret is not configured.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's validation of required API secret configuration during construction.</para>
    /// <para><strong>Data involved:</strong> Configuration that provides ApiSecurity:ApiKey but not ApiSecurity:ApiSecret. The secret is required for signature validation.</para>
    /// <para><strong>Why the data matters:</strong> The API secret is required for request signature validation. Missing secret would prevent signature verification from working.</para>
    /// <para><strong>Expected outcome:</strong> InvalidOperationException is thrown during middleware construction.</para>
    /// <para><strong>Reason for expectation:</strong> The middleware requires both API key and secret to function properly. Missing secret should cause immediate failure with a clear error message.</para>
    /// </remarks>
    [Fact]
    public void Constructor_MissingApiSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration["ApiSecurity:ApiKey"].Returns("test-key");
        _configuration["ApiSecurity:ApiSecret"].Returns((string?)null);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger));
        
        Assert.Contains("ApiSecurity:ApiSecret is required", ex.Message);
    }
}
