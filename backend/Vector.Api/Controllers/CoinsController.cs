using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Coins;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

/// <summary>
/// Controller for coin and achievement management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoinsController : ControllerBase
{
    private readonly ICoinService _coinService;
    private readonly ILogger<CoinsController> _logger;

    public CoinsController(ICoinService coinService, ILogger<CoinsController> logger)
    {
        _coinService = coinService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    /// <summary>
    /// Get current user's coins and rank
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserCoinsDto>> GetMyCoins()
    {
        var userId = GetCurrentUserId();
        var coins = await _coinService.GetUserCoinsAsync(userId);
        return Ok(coins);
    }

    /// <summary>
    /// Get specific user's coins (public endpoint)
    /// </summary>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserCoinsDto>> GetUserCoins(Guid userId)
    {
        var coins = await _coinService.GetUserCoinsAsync(userId);
        return Ok(coins);
    }

    /// <summary>
    /// Get current user's transaction history
    /// </summary>
    [HttpGet("me/transactions")]
    public async Task<ActionResult<List<CoinTransactionDto>>> GetMyTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var userId = GetCurrentUserId();
        var transactions = await _coinService.GetUserTransactionsAsync(userId, page, pageSize);
        return Ok(transactions);
    }

    /// <summary>
    /// Get leaderboard (top N users by coins)
    /// </summary>
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard(
        [FromQuery] int limit = 200)
    {
        if (limit < 1) limit = 200;
        if (limit > 200) limit = 200; // Max 200 users

        var leaderboard = await _coinService.GetLeaderboardAsync(limit);
        return Ok(leaderboard);
    }

    /// <summary>
    /// Get current user's rank
    /// </summary>
    [HttpGet("me/rank")]
    public async Task<ActionResult<object>> GetMyRank()
    {
        var userId = GetCurrentUserId();
        var rank = await _coinService.GetUserRankAsync(userId);
        return Ok(new { rank, displayRank = rank.HasValue ? $"#{rank}" : null });
    }

    /// <summary>
    /// Get all ways to earn coins (achievement definitions)
    /// </summary>
    [HttpGet("achievements")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AchievementDefinitionDto>>> GetAchievements()
    {
        var achievements = await _coinService.GetAchievementDefinitionsAsync();
        return Ok(achievements);
    }

    /// <summary>
    /// Award coins to a user (Internal API - Admin only)
    /// This endpoint is used internally by other services
    /// </summary>
    [HttpPost("award")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CoinTransactionDto>> AwardCoins([FromBody] AwardCoinsRequest request)
    {
        try
        {
            var transaction = await _coinService.AwardCoinsAsync(
                request.UserId,
                request.ActivityType,
                request.Description,
                request.RelatedEntityId,
                request.RelatedEntityType
            );

            return Ok(new CoinTransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                ActivityType = transaction.ActivityType,
                Description = transaction.Description ?? string.Empty,
                CreatedAt = transaction.CreatedAt,
                TimeAgo = "just now"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to award coins: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refresh leaderboard ranks (Admin only background job trigger)
    /// </summary>
    [HttpPost("refresh-ranks")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RefreshLeaderboardRanks()
    {
        await _coinService.RefreshLeaderboardRanksAsync();
        return Ok(new { message = "Leaderboard ranks refreshed successfully" });
    }

    /// <summary>
    /// Create or update an achievement definition (Admin only)
    /// </summary>
    [HttpPost("achievements")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AchievementDefinitionDto>> CreateOrUpdateAchievement(
        [FromBody] CreateAchievementRequest request)
    {
        try
        {
            var achievement = await _coinService.CreateOrUpdateAchievementAsync(
                request.ActivityType,
                request.DisplayName,
                request.CoinsAwarded,
                request.Description,
                request.Icon,
                request.MaxOccurrences
            );

            return Ok(new AchievementDefinitionDto
            {
                ActivityType = achievement.ActivityType,
                DisplayName = achievement.DisplayName,
                Description = achievement.Description,
                CoinsAwarded = achievement.CoinsAwarded,
                Icon = achievement.Icon
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/update achievement");
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request model for creating/updating achievements
/// </summary>
public class CreateAchievementRequest
{
    public string ActivityType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CoinsAwarded { get; set; }
    public string? Icon { get; set; }
    public int? MaxOccurrences { get; set; }
}
