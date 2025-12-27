using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Analytics;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's learning analytics
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAnalytics()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var analytics = await _analyticsService.GetUserAnalyticsAsync(userId.Value);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user analytics");
            return StatusCode(500, new { error = "An error occurred while retrieving analytics." });
        }
    }

    /// <summary>
    /// Get progress for a specific category
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryProgress(string category)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var progress = await _analyticsService.GetCategoryProgressAsync(userId.Value, category);
            // Return empty progress if analytics don't exist yet
            if (progress == null)
            {
                return Ok(new CategoryProgressDto
                {
                    Category = category,
                    QuestionsSolved = 0,
                    TotalQuestions = 0,
                    CompletionPercentage = 0,
                    AverageExecutionTime = 0,
                    AverageMemoryUsed = 0
                });
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category progress");
            return StatusCode(500, new { error = "An error occurred while retrieving category progress." });
        }
    }

    /// <summary>
    /// Get progress for a specific difficulty level
    /// </summary>
    [HttpGet("difficulty/{difficulty}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDifficultyProgress(string difficulty)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var progress = await _analyticsService.GetDifficultyProgressAsync(userId.Value, difficulty);
            // Return empty progress if analytics don't exist yet
            if (progress == null)
            {
                return Ok(new DifficultyProgressDto
                {
                    Difficulty = difficulty,
                    QuestionsSolved = 0,
                    TotalQuestions = 0,
                    CompletionPercentage = 0,
                    AverageExecutionTime = 0,
                    AverageMemoryUsed = 0
                });
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting difficulty progress");
            return StatusCode(500, new { error = "An error occurred while retrieving difficulty progress." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        // JWT tokens may use different claim names
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}

