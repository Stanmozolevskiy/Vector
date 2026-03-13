using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.Models;
using Vector.Api.Services;

namespace Vector.Api.Tests.Services;

public class AnalyticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<AnalyticsService>> _loggerMock;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AnalyticsService>>();
        _analyticsService = new AnalyticsService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserAnalyticsAsync_WithNoAnalytics_ReturnsEmptyDto()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);

        var result = await _analyticsService.GetUserAnalyticsAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(0, result.QuestionsSolved);
        Assert.Equal(0, result.TotalSubmissions);
        Assert.NotNull(result.TotalQuestionsByDifficulty);
    }

    [Fact]
    public async Task GetUserAnalyticsAsync_WithAnalytics_ReturnsData()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);
        var questionId = await SeedQuestionAsync("Arrays", "Easy");
        await SeedUserSolvedQuestionAsync(userId, questionId);

        var analytics = new LearningAnalytics
        {
            UserId = userId,
            QuestionsSolved = 1,
            TotalSubmissions = 2,
            SuccessRate = 50,
            CurrentStreak = 1,
            LongestStreak = 1,
            LastActivityDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.LearningAnalytics.Add(analytics);
        await _context.SaveChangesAsync();

        var result = await _analyticsService.GetUserAnalyticsAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(1, result.QuestionsSolved);
        Assert.Equal(2, result.TotalSubmissions);
        Assert.Equal(50, result.SuccessRate);
    }

    [Fact]
    public async Task GetCategoryProgressAsync_WithNoAnalytics_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);

        var result = await _analyticsService.GetCategoryProgressAsync(userId, "Arrays");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCategoryProgressAsync_WithAnalytics_ReturnsProgress()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);
        var questionId = await SeedQuestionAsync("Arrays", "Easy");
        await SeedUserSolvedQuestionAsync(userId, questionId);

        var analytics = new LearningAnalytics
        {
            UserId = userId,
            QuestionsByCategory = "{\"Arrays\":1}",
            QuestionsByDifficulty = "{}",
            SolutionsByLanguage = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.LearningAnalytics.Add(analytics);
        await _context.SaveChangesAsync();

        var result = await _analyticsService.GetCategoryProgressAsync(userId, "Arrays");

        Assert.NotNull(result);
        Assert.Equal("Arrays", result.Category);
        Assert.Equal(1, result.QuestionsSolved);
    }

    [Fact]
    public async Task GetDifficultyProgressAsync_WithNoAnalytics_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);

        var result = await _analyticsService.GetDifficultyProgressAsync(userId, "Easy");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDifficultyProgressAsync_WithAnalytics_ReturnsProgress()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);
        var questionId = await SeedQuestionAsync("Arrays", "Easy");
        await SeedUserSolvedQuestionAsync(userId, questionId);

        var analytics = new LearningAnalytics
        {
            UserId = userId,
            QuestionsByCategory = "{}",
            QuestionsByDifficulty = "{\"Easy\":1}",
            SolutionsByLanguage = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.LearningAnalytics.Add(analytics);
        await _context.SaveChangesAsync();

        var result = await _analyticsService.GetDifficultyProgressAsync(userId, "Easy");

        Assert.NotNull(result);
        Assert.Equal("Easy", result.Difficulty);
        Assert.Equal(1, result.QuestionsSolved);
    }

    [Fact]
    public async Task UpdateAnalyticsAsync_WithNonExistentQuestion_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);
        var nonExistentQuestionId = Guid.NewGuid();

        await _analyticsService.UpdateAnalyticsAsync(userId, nonExistentQuestionId, "Accepted", 100, 50000, "python");
    }

    [Fact]
    public async Task RebuildAnalyticsAsync_WithNoData_CompletesWithoutError()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId);

        await _analyticsService.RebuildAnalyticsAsync(userId);
    }

    private async Task SeedUserAsync(Guid userId)
    {
        _context.Users.Add(new User
        {
            Id = userId,
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Role = "student",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task<Guid> SeedQuestionAsync(string category, string difficulty)
    {
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question",
            Description = "Test",
            Category = category,
            Difficulty = difficulty,
            QuestionType = "Coding",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.InterviewQuestions.Add(question);
        await _context.SaveChangesAsync();
        return question.Id;
    }

    private async Task SeedUserSolvedQuestionAsync(Guid userId, Guid questionId)
    {
        _context.UserSolvedQuestions.Add(new UserSolvedQuestion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = questionId,
            SolvedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
