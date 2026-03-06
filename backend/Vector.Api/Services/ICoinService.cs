using Vector.Api.DTOs.Coins;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service interface for coin and achievement management
/// </summary>
public interface ICoinService
{
    // Core Points Granting (Robust & Consistent)
    /// <summary>
    /// Award coins to a user for completing an activity.
    /// This is the SINGLE SOURCE OF TRUTH for all coin grants.
    /// </summary>
    Task<CoinTransaction> AwardCoinsAsync(
        Guid userId,
        string activityType,
        string? description = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null);

    // User Coins
    Task<UserCoinsDto> GetUserCoinsAsync(Guid userId);
    Task<List<CoinTransactionDto>> GetUserTransactionsAsync(Guid userId, int page = 1, int pageSize = 50);

    // Leaderboard
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit = 200);
    Task<int?> GetUserRankAsync(Guid userId);
    Task RefreshLeaderboardRanksAsync(); // Background job

    // Achievements
    Task<List<AchievementDefinitionDto>> GetAchievementDefinitionsAsync();
    Task<AchievementDefinition> CreateOrUpdateAchievementAsync(
        string activityType,
        string displayName,
        int coinsAwarded,
        string? description = null,
        string? icon = null,
        int? maxOccurrences = null);
}
