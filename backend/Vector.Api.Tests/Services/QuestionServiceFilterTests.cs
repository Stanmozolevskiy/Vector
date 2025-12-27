using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.DTOs.Question;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class QuestionServiceFilterTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly QuestionService _service;
    private readonly Mock<ILogger<QuestionService>> _loggerMock;

    public QuestionServiceFilterTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<QuestionService>>();
        _service = new QuestionService(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var questions = new[]
        {
            new Vector.Api.Models.InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Two Sum",
                Description = "Find two numbers that add up to target",
                Difficulty = "Easy",
                Category = "Arrays",
                QuestionType = "Coding",
                ApprovalStatus = "Approved",
                IsActive = true,
                CompanyTags = System.Text.Json.JsonSerializer.Serialize(new[] { "Google", "Amazon" }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Vector.Api.Models.InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Valid Parentheses",
                Description = "Check if parentheses are valid",
                Difficulty = "Easy",
                Category = "Strings",
                QuestionType = "Coding",
                ApprovalStatus = "Approved",
                IsActive = true,
                CompanyTags = System.Text.Json.JsonSerializer.Serialize(new[] { "Amazon", "Microsoft" }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Vector.Api.Models.InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Merge Two Sorted Lists",
                Description = "Merge two sorted linked lists",
                Difficulty = "Medium",
                Category = "Linked Lists",
                QuestionType = "Coding",
                ApprovalStatus = "Approved",
                IsActive = true,
                CompanyTags = System.Text.Json.JsonSerializer.Serialize(new[] { "Amazon", "Microsoft" }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Vector.Api.Models.InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Trapping Rain Water",
                Description = "Calculate trapped rainwater",
                Difficulty = "Hard",
                Category = "Arrays",
                QuestionType = "Coding",
                ApprovalStatus = "Approved",
                IsActive = true,
                CompanyTags = System.Text.Json.JsonSerializer.Serialize(new[] { "Google", "Facebook" }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Vector.Api.Models.InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Pending Question",
                Description = "This question is pending",
                Difficulty = "Easy",
                Category = "Arrays",
                QuestionType = "Coding",
                ApprovalStatus = "Pending",
                IsActive = true,
                CompanyTags = System.Text.Json.JsonSerializer.Serialize(new[] { "Google" }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.InterviewQuestions.AddRange(questions);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterByDifficulty_ReturnsOnlyEasyQuestions()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Difficulties = new List<string> { "Easy" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Equal("Easy", q.Difficulty));
        Assert.Equal(2, questions.Count); // Two easy questions (excluding pending)
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterByMultipleDifficulties_ReturnsMatchingQuestions()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Difficulties = new List<string> { "Easy", "Medium" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Contains(q.Difficulty, new[] { "Easy", "Medium" }));
        Assert.Equal(3, questions.Count); // Two easy + one medium
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterByCategory_ReturnsOnlyMatchingCategory()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Categories = new List<string> { "Arrays" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Equal("Arrays", q.Category));
        Assert.Equal(2, questions.Count); // Two array questions (excluding pending)
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterByCompany_ReturnsOnlyMatchingCompanies()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Companies = new List<string> { "Amazon" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q =>
        {
            var tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.CompanyTags ?? "[]");
            Assert.Contains("Amazon", tags ?? new List<string>());
        });
        Assert.Equal(3, questions.Count); // Three questions with Amazon tag: "Two Sum", "Valid Parentheses", "Merge Two Sorted Lists"
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterBySearch_ReturnsMatchingTitles()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Search = "Two Sum"
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Contains("Two", q.Title, StringComparison.OrdinalIgnoreCase));
        Assert.Single(questions);
    }

    [Fact]
    public async Task GetQuestionsAsync_NoFilter_ReturnsOnlyApprovedQuestions()
    {
        // Act
        var result = await _service.GetQuestionsAsync(null);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Equal("Approved", q.ApprovalStatus));
        Assert.Equal(4, questions.Count); // All approved questions
    }

    [Fact]
    public async Task GetQuestionsAsync_CombinedFilters_ReturnsMatchingQuestions()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Difficulties = new List<string> { "Easy" },
            Categories = new List<string> { "Arrays" },
            Companies = new List<string> { "Google" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.Single(questions);
        Assert.Equal("Two Sum", questions[0].Title);
        Assert.Equal("Easy", questions[0].Difficulty);
        Assert.Equal("Arrays", questions[0].Category);
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterByDifficultyCaseInsensitive_Works()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Difficulties = new List<string> { "easy", "EASY", "Easy" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Equal("Easy", q.Difficulty));
        Assert.Equal(2, questions.Count);
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterBySingleDifficulty_ReturnsOnlyMatchingDifficulty()
    {
        // Arrange - Test the exact URL format: difficulties[]=Easy
        var filter = new QuestionFilterDto
        {
            Difficulties = new List<string> { "Easy" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Equal("Easy", q.Difficulty));
        Assert.Equal(2, questions.Count); // Should only return Easy questions, excluding pending
        // Verify no Medium or Hard questions
        Assert.DoesNotContain(questions, q => q.Difficulty == "Medium");
        Assert.DoesNotContain(questions, q => q.Difficulty == "Hard");
    }

    [Fact]
    public async Task GetQuestionsAsync_FilterByCategoryCaseInsensitive_Works()
    {
        // Arrange
        var filter = new QuestionFilterDto
        {
            Categories = new List<string> { "arrays", "ARRAYS", "Arrays" }
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var questions = result.ToList();
        Assert.All(questions, q => Assert.Equal("Arrays", q.Category));
        Assert.Equal(2, questions.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

