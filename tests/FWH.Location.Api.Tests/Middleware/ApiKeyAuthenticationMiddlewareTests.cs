using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FWH.Location.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FWH.Location.Api.Tests.Middleware;

/// <summary>
/// Unit tests for ApiKeyAuthenticationMiddleware in Location API.
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
    /// <para><strong>Data involved:</strong> A valid API key "test-api-key" configured in both middleware and request headers. The request path is /api/locations/nearby, which is not excluded from authentication.</para>
    /// <para><strong>Why the data matters:</strong> Valid API key authentication is the core security mechanism. This test ensures legitimate app requests are not blocked, which is critical for app functionality.</para>
    /// <para><strong>Expected outcome:</strong> The next middleware in the pipeline is called (request proceeds).</para>
    /// <para><strong>Reason for expectation:</strong> When a valid API key is provided, the middleware should authenticate the request and allow it to proceed to the controller. The _next delegate should be invoked exactly once.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsyncValidApiKeyAllowsRequest()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/locations/nearby";
        context.Request.Headers.Add("X-API-Key", apiKey);

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.Received().Invoke(context);
        Assert.Equal(200, context.Response.StatusCode);
    }

    /// <summary>
    /// Tests that requests without API key are rejected with 401 Unauthorized.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The middleware's ability to reject requests missing the required API key header.</para>
    /// <para><strong>Data involved:</strong> A request to /api/locations/nearby without any X-API-Key header. The middleware is configured with apiKey="test-api-key".</para>
    /// <para><strong>Why the data matters:</strong> Missing API keys indicate unauthenticated requests that should be blocked. This test ensures the security mechanism properly rejects unauthorized access attempts.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 401 Unauthorized status code and the next middleware is not called.</para>
    /// <para><strong>Reason for expectation:</strong> Security requires that all API requests include valid authentication. Missing API keys should result in immediate rejection with 401 status, preventing unauthorized access to protected endpoints.</para>
    /// </remarks>
    [Fact]
    public async Task InvokeAsyncMissingApiKeyReturns401()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/locations/nearby";
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
    public async Task InvokeAsyncInvalidApiKeyReturns401()
    {
        // Arrange
        const string apiKey = "test-api-key";
        const string apiSecret = "test-api-secret";
        
        _configuration["ApiSecurity:ApiKey"].Returns(apiKey);
        _configuration["ApiSecurity:ApiSecret"].Returns(apiSecret);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/locations/nearby";
        context.Request.Headers["X-API-Key"] = "wrong-key";

        var middleware = new ApiKeyAuthenticationMiddleware(_next, _configuration, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
