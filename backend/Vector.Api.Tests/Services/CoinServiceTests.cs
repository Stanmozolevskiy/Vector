using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Constants;
using Vector.Api.Data;
using Vector.Api.Models;
using Vector.Api.Services;

namespace Vector.Api.Tests.Services;

public class CoinServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<CoinService>> _loggerMock;
    private readonly CoinService _coinService;

    public CoinServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<CoinService>>();
        _coinService = new CoinService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AwardCoinsAsync_WithValidActivity_CreatesTransactionAndUpdatesTotal()
    {
        var userId = await SeedUserAsync();
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);

        var result = await _coinService.AwardCoinsAsync(userId, AchievementTypes.InterviewCompleted, "Completed interview");

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(10, result.Amount);
        Assert.Equal(AchievementTypes.InterviewCompleted, result.ActivityType);

        var userCoins = await _context.UserCoins.FirstOrDefaultAsync(uc => uc.UserId == userId);
        Assert.NotNull(userCoins);
        Assert.Equal(10, userCoins.TotalCoins);
    }

    [Fact]
    public async Task AwardCoinsAsync_WithInvalidUser_ThrowsException()
    {
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);
        var nonExistentUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _coinService.AwardCoinsAsync(nonExistentUserId, AchievementTypes.InterviewCompleted)
        );
    }

    [Fact]
    public async Task AwardCoinsAsync_WithInvalidActivityType_ThrowsException()
    {
        var userId = await SeedUserAsync();
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _coinService.AwardCoinsAsync(userId, "InvalidActivityType")
        );
    }

    [Fact]
    public async Task AwardCoinsAsync_WithMaxOccurrencesReached_ThrowsException()
    {
        var userId = await SeedUserAsync();
        await SeedAchievementAsync(AchievementTypes.ProfileCompleted, 10, maxOccurrences: 1);

        await _coinService.AwardCoinsAsync(userId, AchievementTypes.ProfileCompleted, "Profile completed");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _coinService.AwardCoinsAsync(userId, AchievementTypes.ProfileCompleted)
        );
    }

    [Fact]
    public async Task GetUserCoinsAsync_WithNoCoins_ReturnsZero()
    {
        var userId = await SeedUserAsync();

        var result = await _coinService.GetUserCoinsAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(0, result.TotalCoins);
        Assert.Equal("0", result.DisplayCoins);
        Assert.Null(result.Rank);
    }

    [Fact]
    public async Task GetUserCoinsAsync_WithCoins_ReturnsData()
    {
        var userId = await SeedUserAsync();
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);
        await _coinService.AwardCoinsAsync(userId, AchievementTypes.InterviewCompleted);

        var result = await _coinService.GetUserCoinsAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(10, result.TotalCoins);
        Assert.Equal("10", result.DisplayCoins);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_ReturnsPaginatedResults()
    {
        var userId = await SeedUserAsync();
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);
        await SeedAchievementAsync(AchievementTypes.FeedbackSubmitted, 10, maxOccurrences: null);
        await _coinService.AwardCoinsAsync(userId, AchievementTypes.InterviewCompleted);
        await _coinService.AwardCoinsAsync(userId, AchievementTypes.FeedbackSubmitted, "Feedback");

        var result = await _coinService.GetUserTransactionsAsync(userId, page: 1, pageSize: 10);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsEmptyList_WhenNoUsers()
    {
        var result = await _coinService.GetLeaderboardAsync(10);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsUsersOrderedByCoins()
    {
        var user1 = await SeedUserAsync();
        var user2 = await SeedUserAsync();
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);
        await _coinService.AwardCoinsAsync(user1, AchievementTypes.InterviewCompleted);
        await _coinService.AwardCoinsAsync(user2, AchievementTypes.InterviewCompleted);
        await _coinService.AwardCoinsAsync(user2, AchievementTypes.InterviewCompleted);

        var result = await _coinService.GetLeaderboardAsync(10);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].TotalCoins >= result[1].TotalCoins);
    }

    [Fact]
    public async Task GetAchievementDefinitionsAsync_ReturnsSeededAchievements()
    {
        await SeedAchievementAsync(AchievementTypes.InterviewCompleted, 10, maxOccurrences: null);
        await SeedAchievementAsync(AchievementTypes.FeedbackSubmitted, 10, maxOccurrences: null);

        var result = await _coinService.GetAchievementDefinitionsAsync();

        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
        Assert.Contains(result, a => a.ActivityType == AchievementTypes.InterviewCompleted);
        Assert.Contains(result, a => a.ActivityType == AchievementTypes.FeedbackSubmitted);
    }

    [Fact]
    public async Task GetUserRankAsync_WithNoCoins_ReturnsNull()
    {
        var userId = await SeedUserAsync();

        var result = await _coinService.GetUserRankAsync(userId);

        Assert.Null(result);
    }

    private async Task<Guid> SeedUserAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    private async Task SeedAchievementAsync(string activityType, int coinsAwarded, int? maxOccurrences)
    {
        if (await _context.AchievementDefinitions.AnyAsync(a => a.ActivityType == activityType))
            return;

        _context.AchievementDefinitions.Add(new AchievementDefinition
        {
            ActivityType = activityType,
            DisplayName = activityType,
            Description = $"Test {activityType}",
            CoinsAwarded = coinsAwarded,
            Icon = "🪙",
            IsActive = true,
            MaxOccurrences = maxOccurrences
        });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
