using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisService _redisService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext context, IRedisService redisService, ILogger<HealthController> logger)
    {
        _context = context;
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Vector API"
        });
    }

    /// <summary>
    /// Detailed health check with database and Redis status
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var dbHealth = await CheckDatabaseHealthAsync();
        var redisHealth = await CheckRedisHealthAsync();

        var allHealthy = dbHealth.Healthy && redisHealth.Healthy;

        var health = new
        {
            status = allHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "Vector API",
            components = new
            {
                database = new
                {
                    healthy = dbHealth.Healthy,
                    responseTimeMs = dbHealth.ResponseTimeMs,
                    message = dbHealth.Message
                },
                redis = new
                {
                    healthy = redisHealth.Healthy,
                    responseTimeMs = redisHealth.ResponseTimeMs,
                    message = redisHealth.Message
                }
            }
        };

        return allHealthy 
            ? Ok(health) 
            : StatusCode(503, health); // Service Unavailable if any component unhealthy
    }

    private async Task<HealthCheckResult> CheckDatabaseHealthAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var responseTime = DateTime.UtcNow;
            await _context.Users.Take(1).ToListAsync(); // Test query
            var queryTime = (DateTime.UtcNow - responseTime).TotalMilliseconds;

            return new HealthCheckResult
            {
                Healthy = canConnect,
                ResponseTimeMs = queryTime,
                Message = canConnect ? "Database connection successful" : "Database connection failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new HealthCheckResult
            {
                Healthy = false,
                ResponseTimeMs = 0,
                Message = $"Database error: {ex.Message}"
            };
        }
    }

    private async Task<HealthCheckResult> CheckRedisHealthAsync()
    {
        try
        {
            var responseTime = DateTime.UtcNow;
            var isHealthy = await _redisService.IsHealthyAsync();
            var queryTime = (DateTime.UtcNow - responseTime).TotalMilliseconds;

            return new HealthCheckResult
            {
                Healthy = isHealthy,
                ResponseTimeMs = queryTime,
                Message = isHealthy ? "Redis connection successful" : "Redis connection slow or unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return new HealthCheckResult
            {
                Healthy = false,
                ResponseTimeMs = 0,
                Message = $"Redis error: {ex.Message}"
            };
        }
    }

    private class HealthCheckResult
    {
        public bool Healthy { get; set; }
        public double ResponseTimeMs { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

