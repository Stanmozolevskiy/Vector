using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.Coins;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service for managing coins and achievements
/// </summary>
public class CoinService : ICoinService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CoinService> _logger;

    public CoinService(ApplicationDbContext context, ILogger<CoinService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Robust and consistent method to award coins for any activity.
    /// This is the SINGLE SOURCE OF TRUTH for all coin grants.
    /// </summary>
    public async Task<CoinTransaction> AwardCoinsAsync(
        Guid userId,
        string activityType,
        string? description = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        // 1. Validate user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when awarding coins", userId);
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // 2. Get achievement definition
        var achievement = await _context.AchievementDefinitions
            .FirstOrDefaultAsync(a => a.ActivityType == activityType && a.IsActive);

        if (achievement == null)
        {
            _logger.LogWarning("No active achievement found for activity type: {ActivityType}", activityType);
            throw new InvalidOperationException($"No achievement definition for {activityType}");
        }

        // 3. Check max occurrences (if limited)
        if (achievement.MaxOccurrences.HasValue)
        {
            var existingCount = await _context.CoinTransactions
                .CountAsync(t => t.UserId == userId && t.ActivityType == activityType);

            if (existingCount >= achievement.MaxOccurrences.Value)
            {
                _logger.LogInformation(
                    "User {UserId} has reached max occurrences ({Max}) for {ActivityType}",
                    userId, achievement.MaxOccurrences.Value, activityType);
                throw new InvalidOperationException(
                    $"Maximum {achievement.MaxOccurrences} occurrences reached for {activityType}");
            }
        }

        // 4. Create transaction
        var transaction = new CoinTransaction
        {
            UserId = userId,
            Amount = achievement.CoinsAwarded,
            ActivityType = activityType,
            Description = description ?? achievement.Description ?? achievement.DisplayName,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow
        };

        _context.CoinTransactions.Add(transaction);

        // 5. Update user's total coins
        var userCoins = await _context.UserCoins
            .FirstOrDefaultAsync(uc => uc.UserId == userId);

        if (userCoins == null)
        {
            userCoins = new UserCoins
            {
                UserId = userId,
                TotalCoins = achievement.CoinsAwarded,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserCoins.Add(userCoins);
        }
        else
        {
            userCoins.TotalCoins += achievement.CoinsAwarded;
            userCoins.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Awarded {Coins} coins to user {UserId} for {ActivityType}",
            achievement.CoinsAwarded, userId, activityType);

        return transaction;
    }

    public async Task<UserCoinsDto> GetUserCoinsAsync(Guid userId)
    {
        var userCoins = await _context.UserCoins
            .FirstOrDefaultAsync(uc => uc.UserId == userId);

        if (userCoins == null)
        {
            return new UserCoinsDto
            {
                UserId = userId,
                TotalCoins = 0,
                DisplayCoins = "0",
                Rank = null,
                DisplayRank = null
            };
        }

        return new UserCoinsDto
        {
            UserId = userId,
            TotalCoins = userCoins.TotalCoins,
            DisplayCoins = FormatCoins(userCoins.TotalCoins),
            Rank = userCoins.Rank,
            DisplayRank = userCoins.Rank.HasValue ? $"#{userCoins.Rank}" : null
        };
    }

    public async Task<List<CoinTransactionDto>> GetUserTransactionsAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        var transactions = await _context.CoinTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return transactions.Select(t => new CoinTransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            ActivityType = t.ActivityType,
            Description = t.Description ?? string.Empty,
            CreatedAt = t.CreatedAt,
            TimeAgo = FormatTimeAgo(t.CreatedAt)
        }).ToList();
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit = 200)
    {
        var leaderboard = await _context.UserCoins
            .Include(uc => uc.User)
            .OrderByDescending(uc => uc.TotalCoins)
            .Take(limit)
            .ToListAsync();

        return leaderboard.Select((uc, index) => new LeaderboardEntryDto
        {
            Rank = index + 1,
            UserId = uc.UserId,
            FirstName = uc.User.FirstName,
            LastName = uc.User.LastName,
            ProfilePictureUrl = uc.User.ProfilePictureUrl,
            TotalCoins = uc.TotalCoins,
            DisplayCoins = FormatCoins(uc.TotalCoins)
        }).ToList();
    }

    public async Task<int?> GetUserRankAsync(Guid userId)
    {
        var userCoins = await _context.UserCoins
            .FirstOrDefaultAsync(uc => uc.UserId == userId);

        if (userCoins == null) return null;

        var rank = await _context.UserCoins
            .CountAsync(uc => uc.TotalCoins > userCoins.TotalCoins) + 1;

        return rank;
    }

    public async Task RefreshLeaderboardRanksAsync()
    {
        var allUserCoins = await _context.UserCoins
            .OrderByDescending(uc => uc.TotalCoins)
            .ToListAsync();

        for (int i = 0; i < allUserCoins.Count; i++)
        {
            allUserCoins[i].Rank = i + 1;
            allUserCoins[i].LastRankUpdate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Refreshed leaderboard ranks for {Count} users", allUserCoins.Count);
    }

    public async Task<List<AchievementDefinitionDto>> GetAchievementDefinitionsAsync()
    {
        var achievements = await _context.AchievementDefinitions
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CoinsAwarded)
            .ToListAsync();

        return achievements.Select(a => new AchievementDefinitionDto
        {
            ActivityType = a.ActivityType,
            DisplayName = a.DisplayName,
            Description = a.Description,
            CoinsAwarded = a.CoinsAwarded,
            Icon = a.Icon
        }).ToList();
    }

    public async Task<AchievementDefinition> CreateOrUpdateAchievementAsync(
        string activityType,
        string displayName,
        int coinsAwarded,
        string? description = null,
        string? icon = null,
        int? maxOccurrences = null)
    {
        var existing = await _context.AchievementDefinitions
            .FirstOrDefaultAsync(a => a.ActivityType == activityType);

        if (existing != null)
        {
            existing.DisplayName = displayName;
            existing.CoinsAwarded = coinsAwarded;
            existing.Description = description;
            existing.Icon = icon;
            existing.MaxOccurrences = maxOccurrences;
            existing.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Updated achievement definition: {ActivityType}", activityType);
        }
        else
        {
            existing = new AchievementDefinition
            {
                ActivityType = activityType,
                DisplayName = displayName,
                CoinsAwarded = coinsAwarded,
                Description = description,
                Icon = icon,
                MaxOccurrences = maxOccurrences,
                IsActive = true
            };
            _context.AchievementDefinitions.Add(existing);
            
            _logger.LogInformation("Created achievement definition: {ActivityType}", activityType);
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    // Helper Methods
    private string FormatCoins(int coins)
    {
        if (coins >= 1000000)
            return $"{coins / 1000000.0:0.#}M";
        if (coins >= 1000)
            return $"{coins / 1000.0:0.#}k";
        return coins.ToString();
    }

    private string FormatTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalDays >= 365)
            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        if (timeSpan.TotalDays >= 30)
            return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        return "just now";
    }
}
