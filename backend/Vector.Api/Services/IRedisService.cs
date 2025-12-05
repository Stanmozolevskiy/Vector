namespace Vector.Api.Services;

/// <summary>
/// Redis service for caching, session management, and rate limiting
/// </summary>
public interface IRedisService
{
    // Token Management
    Task<bool> StoreRefreshTokenAsync(Guid userId, string token, TimeSpan expiration);
    Task<string?> GetRefreshTokenAsync(Guid userId);
    Task<bool> RevokeRefreshTokenAsync(Guid userId);
    Task<bool> IsTokenBlacklistedAsync(string token);
    Task<bool> BlacklistTokenAsync(string token, TimeSpan expiration);

    // Session Caching
    Task<bool> CacheUserSessionAsync(Guid userId, object userData, TimeSpan expiration);
    Task<T?> GetCachedUserSessionAsync<T>(Guid userId) where T : class;
    Task<bool> InvalidateUserSessionAsync(Guid userId);

    // Rate Limiting
    Task<bool> CheckRateLimitAsync(string key, int maxAttempts, TimeSpan window);
    Task<int> GetRateLimitAttemptsAsync(string key);
    Task<bool> ResetRateLimitAsync(string key);

    // Health Check
    Task<bool> IsHealthyAsync();
    
    // Generic Cache Operations
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
}

