using System;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Common.Location.RateLimiting;

/// <summary>
/// Token bucket rate limiter implementation.
/// Limits the rate of API calls to respect external service limits.
/// </summary>
public class TokenBucketRateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private int _availableTokens;
    private DateTime _lastRefillTime;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new token bucket rate limiter.
    /// </summary>
    /// <param name="maxTokens">Maximum number of tokens (requests) in the bucket</param>
    /// <param name="refillInterval">Time interval to refill one token</param>
    public TokenBucketRateLimiter(int maxTokens, TimeSpan refillInterval)
    {
        if (maxTokens <= 0)
            throw new ArgumentException("Max tokens must be positive", nameof(maxTokens));
        
        if (refillInterval <= TimeSpan.Zero)
            throw new ArgumentException("Refill interval must be positive", nameof(refillInterval));

        _maxTokens = maxTokens;
        _refillInterval = refillInterval;
        _availableTokens = maxTokens;
        _lastRefillTime = DateTime.UtcNow;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Waits until a token is available, then consumes it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when a token is consumed</returns>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await RefillTokensAsync();
            
            while (_availableTokens <= 0)
            {
                var timeSinceLastRefill = DateTime.UtcNow - _lastRefillTime;
                var timeToWait = _refillInterval - timeSinceLastRefill;
                
                if (timeToWait > TimeSpan.Zero)
                {
                    await Task.Delay(timeToWait, cancellationToken);
                }
                
                await RefillTokensAsync();
            }

            _availableTokens--;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Tries to consume a token without waiting.
    /// </summary>
    /// <returns>True if a token was available and consumed, false otherwise</returns>
    public bool TryConsume()
    {
        lock (_lock)
        {
            RefillTokens();
            
            if (_availableTokens > 0)
            {
                _availableTokens--;
                return true;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Gets the number of currently available tokens.
    /// </summary>
    public int AvailableTokens
    {
        get
        {
            lock (_lock)
            {
                RefillTokens();
                return _availableTokens;
            }
        }
    }

    private Task RefillTokensAsync()
    {
        lock (_lock)
        {
            RefillTokens();
        }
        return Task.CompletedTask;
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastRefill = now - _lastRefillTime;
        
        if (timeSinceLastRefill >= _refillInterval)
        {
            var tokensToAdd = (int)(timeSinceLastRefill.TotalMilliseconds / _refillInterval.TotalMilliseconds);
            _availableTokens = Math.Min(_availableTokens + tokensToAdd, _maxTokens);
            _lastRefillTime = now;
        }
    }
}
