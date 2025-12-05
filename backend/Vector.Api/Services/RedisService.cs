using StackExchange.Redis;
using System.Text.Json;

namespace Vector.Api.Services;

/// <summary>
/// Redis service implementation for caching, tokens, and rate limiting
/// Uses connection pooling via IConnectionMultiplexer (singleton)
/// </summary>
public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisService> _logger;
    private const string REFRESH_TOKEN_PREFIX = "rt:";
    private const string BLACKLIST_PREFIX = "bl:";
    private const string SESSION_PREFIX = "session:";
    private const string RATE_LIMIT_PREFIX = "rl:";

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
    }

    #region Token Management

    public async Task<bool> StoreRefreshTokenAsync(Guid userId, string token, TimeSpan expiration)
    {
        try
        {
            var key = $"{REFRESH_TOKEN_PREFIX}{userId}";
            return await _db.StringSetAsync(key, token, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string?> GetRefreshTokenAsync(Guid userId)
    {
        try
        {
            var key = $"{REFRESH_TOKEN_PREFIX}{userId}";
            return await _db.StringGetAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get refresh token for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(Guid userId)
    {
        try
        {
            var key = $"{REFRESH_TOKEN_PREFIX}{userId}";
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        try
        {
            var key = $"{BLACKLIST_PREFIX}{token}";
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if token is blacklisted");
            return false;
        }
    }

    public async Task<bool> BlacklistTokenAsync(string token, TimeSpan expiration)
    {
        try
        {
            var key = $"{BLACKLIST_PREFIX}{token}";
            return await _db.StringSetAsync(key, "revoked", expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to blacklist token");
            return false;
        }
    }

    #endregion

    #region Session Caching

    public async Task<bool> CacheUserSessionAsync(Guid userId, object userData, TimeSpan expiration)
    {
        try
        {
            var key = $"{SESSION_PREFIX}{userId}";
            var json = JsonSerializer.Serialize(userData);
            return await _db.StringSetAsync(key, json, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache user session for {UserId}", userId);
            return false;
        }
    }

    public async Task<T?> GetCachedUserSessionAsync<T>(Guid userId) where T : class
    {
        try
        {
            var key = $"{SESSION_PREFIX}{userId}";
            var json = await _db.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<T>(json.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached session for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> InvalidateUserSessionAsync(Guid userId)
    {
        try
        {
            var key = $"{SESSION_PREFIX}{userId}";
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate session for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Rate Limiting

    public async Task<bool> CheckRateLimitAsync(string key, int maxAttempts, TimeSpan window)
    {
        try
        {
            var rateLimitKey = $"{RATE_LIMIT_PREFIX}{key}";
            var current = await _db.StringGetAsync(rateLimitKey);

            if (current.IsNullOrEmpty)
            {
                // First attempt
                await _db.StringSetAsync(rateLimitKey, "1", window);
                return true;
            }

            var attempts = int.Parse(current.ToString());
            if (attempts >= maxAttempts)
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
                return false;
            }

            // Increment attempt counter
            await _db.StringIncrementAsync(rateLimitKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check rate limit for key {Key}", key);
            return true; // Fail open - allow request
        }
    }

    public async Task<int> GetRateLimitAttemptsAsync(string key)
    {
        try
        {
            var rateLimitKey = $"{RATE_LIMIT_PREFIX}{key}";
            var current = await _db.StringGetAsync(rateLimitKey);
            return current.IsNullOrEmpty ? 0 : int.Parse(current.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rate limit attempts for key {Key}", key);
            return 0;
        }
    }

    public async Task<bool> ResetRateLimitAsync(string key)
    {
        try
        {
            var rateLimitKey = $"{RATE_LIMIT_PREFIX}{key}";
            return await _db.KeyDeleteAsync(rateLimitKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset rate limit for key {Key}", key);
            return false;
        }
    }

    #endregion

    #region Generic Cache Operations

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            if (expiration.HasValue)
            {
                return await _db.StringSetAsync(key, json, expiration.Value);
            }
            else
            {
                return await _db.StringSetAsync(key, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache key {Key}", key);
            return false;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await _db.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(json.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache key {Key}", key);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cache key {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if key exists {Key}", key);
            return false;
        }
    }

    #endregion

    #region Health Check

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var ping = await _db.PingAsync();
            var isHealthy = ping.TotalMilliseconds < 1000; // Healthy if responds within 1 second
            
            if (!isHealthy)
            {
                _logger.LogWarning("Redis health check slow: {PingMs}ms", ping.TotalMilliseconds);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return false;
        }
    }

    #endregion
}

