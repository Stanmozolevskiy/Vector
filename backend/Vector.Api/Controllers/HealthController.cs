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

        var allHealthy = dbHealth.healthy && redisHealth.healthy;

        var health = new
        {
            status = allHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "Vector API",
            components = new
            {
                database = dbHealth,
                redis = redisHealth
            }
        };

        return allHealthy 
            ? Ok(health) 
            : StatusCode(503, health); // Service Unavailable if any component unhealthy
    }

    private async Task<(bool healthy, double responseTimeMs, string message)> CheckDatabaseHealthAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var responseTime = DateTime.UtcNow;
            await _context.Users.Take(1).ToListAsync(); // Test query
            var queryTime = (DateTime.UtcNow - responseTime).TotalMilliseconds;

            return (
                healthy: canConnect,
                responseTimeMs: queryTime,
                message: canConnect ? "Database connection successful" : "Database connection failed"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return (
                healthy: false,
                responseTimeMs: 0,
                message: $"Database error: {ex.Message}"
            );
        }
    }

    private async Task<(bool healthy, double responseTimeMs, string message)> CheckRedisHealthAsync()
    {
        try
        {
            var responseTime = DateTime.UtcNow;
            var isHealthy = await _redisService.IsHealthyAsync();
            var queryTime = (DateTime.UtcNow - responseTime).TotalMilliseconds;

            return (
                healthy: isHealthy,
                responseTimeMs: queryTime,
                message: isHealthy ? "Redis connection successful" : "Redis connection slow or unavailable"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return (
                healthy: false,
                responseTimeMs: 0,
                message: $"Redis error: {ex.Message}"
            );
        }
    }
}

