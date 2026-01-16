# Overpass Location Service Resilience Implementation

## Summary

Added Polly-based resilience patterns to the `OverpassLocationService` to handle transient failures when calling the Overpass API. The implementation includes retry logic, circuit breaker, timeout policies, and a **fallback handler** to provide graceful degradation.

## Changes Made

### 1. Package Dependencies

Added the following packages to `src/FWH.Common.Location/FWH.Common.Location.csproj`:
- `Microsoft.Extensions.Http.Resilience` (v10.2.0) - Standard resilience patterns
- `Polly.Extensions` (v8.5.0) - Advanced Polly features including fallback

### 2. Service Registration Updates

Updated `src/FWH.Common.Location/Extensions/LocationServiceCollectionExtensions.cs` to configure a custom resilience pipeline with four layers:

```csharp
services.AddHttpClient<OverpassLocationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FunWasHad/1.0");
})
.AddResilienceHandler("overpass-pipeline", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // 1. Fallback (outer layer - executed last if all else fails)
    builder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
            .Handle<BrokenCircuitException>()
            .HandleResult(response => !response.IsSuccessStatusCode),
        FallbackAction = args =>
        {
            // Return an empty JSON response
            var fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""elements"":[]}", System.Text.Encoding.UTF8, "application/json")
            };
            
            return Outcome.FromResultAsValueTask(fallbackResponse);
        }
    });
    
    // 2. Circuit breaker
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        SamplingDuration = TimeSpan.FromSeconds(30),
        FailureRatio = 0.5,
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(15)
    });
    
    // 3. Retry with exponential backoff
    builder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = Polly.DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromSeconds(1)
    });
    
    // 4. Timeout per attempt
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

### 3. Service Implementation Updates

Updated `src/FWH.Common.Location/Services/OverpassLocationService.cs`:
- Added `.ConfigureAwait(false)` to async calls for better performance
- Updated log messages to indicate resilience policies are active
- Added comments documenting the automatic retry behavior

### 4. Tests

Created comprehensive tests in `tests/FWH.Common.Location.Tests/Services/OverpassLocationServiceResilienceTests.cs`:
- `GetNearbyBusinessesAsync_WithTransientFailure_RetriesAndSucceeds`: Verifies retry logic works
- `GetNearbyBusinessesAsync_WithValidCoordinates_ReturnsBusinesses`: Tests successful requests
- `GetNearbyBusinessesAsync_WithInvalidLatitude_ThrowsArgumentOutOfRangeException`: Validates input
- `GetNearbyBusinessesAsync_WithInvalidLongitude_ThrowsArgumentOutOfRangeException`: Validates input

## Resilience Pipeline Layers

The resilience pipeline is configured with four layers, executed in this order (outer to inner):

### 1. Fallback Handler (NEW!)
- **Purpose**: Provides graceful degradation when all else fails
- **Triggers**: Executes when:
  - All retries are exhausted
  - Circuit breaker is open (broken)
  - Unhandled exceptions occur (HttpRequestException, TimeoutException)
  - HTTP response is unsuccessful
- **Action**: Returns an empty JSON response `{"elements":[]}` with HTTP 200 OK
- **Benefit**: Application continues to function even when the Overpass API is completely unavailable

### 2. Circuit Breaker
- **Sampling Duration**: 30 seconds
- **Failure Ratio**: 50% (opens if 50% of requests fail)
- **Minimum Throughput**: 3 requests minimum before circuit can break
- **Break Duration**: 15 seconds (circuit stays open for 15s before attempting half-open)
- **Behavior**: Prevents cascading failures by "opening" the circuit when too many failures occur

### 3. Retry Policy
- **Max Attempts**: 3 retries (4 total attempts)
- **Backoff**: Exponential with jitter
- **Base Delay**: 1 second
- **Behavior**: Automatically retries on:
  - Transient HTTP errors (5xx status codes except 404)
  - Network failures (timeouts, connection errors)
  - Request timeouts

### 4. Timeout Policy
- **Per-Attempt Timeout**: 10 seconds (each retry attempt times out after 10s)
- **Total HttpClient Timeout**: 30 seconds (configured on HttpClient itself)

## Resilience Pipeline Flow

```
Request → Fallback → Circuit Breaker → Retry → Timeout → HTTP Call
                                          ↓         ↓
                                       (retry)  (timeout)
                                          ↓
                                        Success or Exception
                                          ↓
                            ← ← ← ← Return through layers ← ← ← ←
```

When a failure occurs:
1. **Timeout** triggers first (10s per attempt)
2. **Retry** catches the timeout and tries again (up to 3 retries)
3. **Circuit Breaker** monitors the pattern of failures
4. **Fallback** executes if all retries fail or circuit is open

## Benefits

1. **Improved Reliability**: Automatically recovers from transient failures
2. **Graceful Degradation**: Fallback ensures the app continues to work with empty results
3. **Better User Experience**: Reduces failed requests and provides consistent behavior
4. **Service Protection**: Circuit breaker prevents overwhelming a degraded service
5. **Observability**: Resilience events are logged automatically
6. **Standard Patterns**: Uses Microsoft + Polly's recommended resilience implementation

## Configuration Examples

### For Testing (Faster, Disable Circuit Breaker)
```csharp
.AddResilienceHandler("overpass-pipeline", builder =>
{
    builder.AddFallback(/* same config */);
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        SamplingDuration = TimeSpan.FromDays(1) // Effectively disabled
    });
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 2,
        Delay = TimeSpan.FromMilliseconds(10)
    });
    builder.AddTimeout(TimeSpan.FromSeconds(30));
});
```

### For Production (More Aggressive)
```csharp
.AddResilienceHandler("overpass-pipeline", builder =>
{
    builder.AddFallback(/* same config */);
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        BreakDuration = TimeSpan.FromMinutes(1),
        FailureRatio = 0.3 // Break earlier
    });
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(2)
    });
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

## Monitoring

The resilience policies emit telemetry that can be observed through:
- Application Insights (if configured)
- OpenTelemetry traces
- Standard .NET logging (via `ILogger`)
- Console output when fallback is triggered

## References

- [Microsoft.Extensions.Http.Resilience Documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [Polly Documentation](https://www.pollydocs.org/)
- [Polly Fallback Strategy](https://www.pollydocs.org/strategies/fallback)
- [.NET Resilience Patterns](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/)

