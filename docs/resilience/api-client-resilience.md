# API Client Resilience Implementation

## Summary

Added Polly-based resilience patterns to the `LocationApiClient` and `MarketingApiClient` to handle transient failures when calling internal FunWasHad APIs. This ensures that line 135 in `LocationApiClient.cs` (and all other HTTP calls) use the retry pipeline with fallback handling.

## Changes Made

### 1. Package Dependencies

Added the following packages to `src/FWH.Orchestrix.Mediator.Remote/FWH.Orchestrix.Mediator.Remote.csproj`:
- `Microsoft.Extensions.Http.Resilience` (v10.2.0) - Standard resilience patterns
- `Polly.Extensions` (v8.5.0) - Advanced Polly features including fallback

### 2. HttpClient Registration Updates

Updated `src/FWH.Orchestrix.Mediator.Remote/Extensions/MediatorServiceCollectionExtensions.cs` to add resilience pipelines to both API clients:

#### Location API Client
```csharp
services.AddHttpClient("LocationApi", client =>
{
    client.BaseAddress = new Uri(options.LocationApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddResilienceHandler("location-api-pipeline", builder =>
{
    // 1. Fallback - returns empty JSON array on failure
    // 2. Circuit breaker - prevents cascading failures
    // 3. Retry - 3 retries with exponential backoff
    // 4. Timeout - 10 seconds per attempt
});
```

#### Marketing API Client
```csharp
services.AddHttpClient("MarketingApi", client =>
{
    client.BaseAddress = new Uri(options.MarketingApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddResilienceHandler("marketing-api-pipeline", builder =>
{
    // Same 4-layer resilience pipeline
});
```

### 3. Impact on LocationApiClient

The `LocationApiClient` at line 135 now benefits from the resilience pipeline:

```csharp
// Line 135 in LocationApiClient.cs
using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
```

When this line executes, the request passes through:
1. **Fallback Handler** - Catches all failures and returns empty JSON `[]`
2. **Circuit Breaker** - Opens after 50% failure rate
3. **Retry Logic** - Up to 3 retries with exponential backoff
4. **Timeout** - 10 seconds per attempt

## Resilience Pipeline Configuration

Both API clients use identical 4-layer resilience pipelines:

### Layer 1: Fallback Handler
- **Purpose**: Graceful degradation when all else fails
- **Triggers**: After all retries exhausted, circuit open, or unhandled exceptions
- **Action**: Returns HTTP 200 with empty JSON array `[]`
- **Benefit**: Application continues functioning with empty data rather than crashing

### Layer 2: Circuit Breaker
- **Sampling Duration**: 30 seconds
- **Failure Ratio**: 50% (opens after half of requests fail)
- **Minimum Throughput**: 3 requests before circuit can break
- **Break Duration**: 15 seconds
- **Benefit**: Prevents overwhelming degraded downstream services

### Layer 3: Retry with Exponential Backoff
- **Max Attempts**: 3 retries (4 total attempts)
- **Backoff**: Exponential with jitter
- **Base Delay**: 1 second
- **Excludes**: 404 Not Found responses (no retry)
- **Benefit**: Automatically recovers from transient failures

### Layer 4: Timeout
- **Per-Attempt Timeout**: 10 seconds
- **Total HttpClient Timeout**: 30 seconds
- **Benefit**: Prevents hanging requests

## Request Flow Example

```
LocationApiClient.GetNearbyBusinessesAsync() called
    ↓
Line 135: _httpClient.GetAsync(requestUri, cancellationToken)
    ↓
    ┌─ Fallback Handler ─┐
    │  ┌─ Circuit Breaker ─┐
    │  │  ┌─ Retry Logic ─┐
    │  │  │  ┌─ Timeout ─┐
    │  │  │  │           │
    │  │  │  │  HTTP     │
    │  │  │  │  Request  │
    │  │  │  │           │
    │  │  │  └───────────┘
    │  │  └───────────────┘
    │  └───────────────────┘
    └───────────────────────┘
    ↓
Response (or fallback empty array)
```

## Benefits

1. **Improved Reliability**: Automatically recovers from transient API failures
2. **Graceful Degradation**: Fallback ensures UI doesn't crash, shows empty state instead
3. **Better UX**: Users experience fewer errors and loading states
4. **Service Protection**: Circuit breaker prevents cascading failures across services
5. **Consistent Behavior**: Both Location and Marketing APIs use same patterns
6. **Observable**: All resilience events are logged automatically

## Configuration Examples

### For Development (Faster Testing)
```csharp
.AddResilienceHandler("location-api-pipeline", builder =>
{
    builder.AddFallback(/* same config */);
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        SamplingDuration = TimeSpan.FromDays(1) // Effectively disabled
    });
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 1,
        Delay = TimeSpan.FromMilliseconds(10)
    });
    builder.AddTimeout(TimeSpan.FromSeconds(5));
});
```

### For Production (More Aggressive)
```csharp
.AddResilienceHandler("location-api-pipeline", builder =>
{
    builder.AddFallback(/* same config */);
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        BreakDuration = TimeSpan.FromMinutes(1),
        FailureRatio = 0.3 // Break earlier at 30%
    });
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(2)
    });
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

## Testing

The resilience pipeline can be tested by:
1. **Stopping the Location API** - Should return empty results via fallback
2. **Introducing network delays** - Should retry and eventually succeed
3. **Simulating high failure rates** - Should trigger circuit breaker
4. **Request timeouts** - Should timeout and retry

## Monitoring

Resilience events can be monitored through:
- Application Insights (if configured)
- OpenTelemetry traces
- .NET logging (`ILogger`)
- Polly telemetry events

## Related Documentation

- [Overpass Service Resilience](overpass-service-resilience.md) - Similar implementation for external API
- [Microsoft Resilience Documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [Polly Documentation](https://www.pollydocs.org/)

## Migration Notes

**Before**: HTTP calls would throw exceptions on failure
```csharp
// Would throw HttpRequestException on network failure
var response = await _httpClient.GetAsync(requestUri, cancellationToken);
```

**After**: HTTP calls automatically retry and fall back gracefully
```csharp
// Retries 3 times, then returns empty JSON if all fail
var response = await _httpClient.GetAsync(requestUri, cancellationToken);
```

No code changes required in `LocationApiClient` - resilience is applied at the HttpClient registration level, making all HTTP calls resilient automatically.
