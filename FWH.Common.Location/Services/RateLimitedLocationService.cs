using Microsoft.Extensions.Logging;
using FWH.Common.Location.Models;
using FWH.Common.Location.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Common.Location.Services;

/// <summary>
/// Decorator for ILocationService that adds rate limiting.
/// Protects external APIs from excessive requests.
/// </summary>
public class RateLimitedLocationService : ILocationService
{
    private readonly ILocationService _innerService;
    private readonly TokenBucketRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitedLocationService> _logger;

    /// <summary>
    /// Creates a rate-limited location service.
    /// Default: 10 requests per minute (respects Overpass API fair use policy).
    /// </summary>
    public RateLimitedLocationService(
        ILocationService innerService,
        ILogger<RateLimitedLocationService> logger,
        int maxRequestsPerMinute = 10)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Token bucket: maxRequestsPerMinute tokens, refill 1 token every (60/max) seconds
        var refillInterval = TimeSpan.FromSeconds(60.0 / maxRequestsPerMinute);
        _rateLimiter = new TokenBucketRateLimiter(maxRequestsPerMinute, refillInterval);
    }

    public async Task<IEnumerable<BusinessLocation>> GetNearbyBusinessesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        IEnumerable<string>? categories = null,
        CancellationToken cancellationToken = default)
    {
        // Validate coordinates
        ValidateCoordinates(latitude, longitude);
        
        var availableTokens = _rateLimiter.AvailableTokens;
        _logger.LogDebug("Rate limiter status: {AvailableTokens} tokens available", availableTokens);

        // Wait for rate limit
        await _rateLimiter.WaitAsync(cancellationToken);
        
        _logger.LogDebug("Rate limit passed, executing GetNearbyBusinessesAsync");
        return await _innerService.GetNearbyBusinessesAsync(
            latitude, 
            longitude, 
            radiusMeters, 
            categories, 
            cancellationToken);
    }

    public async Task<BusinessLocation?> GetClosestBusinessAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        // Validate coordinates
        ValidateCoordinates(latitude, longitude);
        
        var availableTokens = _rateLimiter.AvailableTokens;
        _logger.LogDebug("Rate limiter status: {AvailableTokens} tokens available", availableTokens);

        // Wait for rate limit
        await _rateLimiter.WaitAsync(cancellationToken);
        
        _logger.LogDebug("Rate limit passed, executing GetClosestBusinessAsync");
        return await _innerService.GetClosestBusinessAsync(
            latitude, 
            longitude, 
            maxDistanceMeters, 
            cancellationToken);
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude), 
                latitude, 
                "Latitude must be between -90 and 90 degrees");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude), 
                longitude, 
                "Longitude must be between -180 and 180 degrees");
        }
    }
}
