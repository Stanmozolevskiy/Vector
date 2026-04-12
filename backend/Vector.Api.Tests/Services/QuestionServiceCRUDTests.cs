using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.DTOs.Question;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class QuestionServiceCRUDTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<QuestionService>> _loggerMock;
    private readonly Mock<ICoinService> _coinServiceMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly QuestionService _service;

    public QuestionServiceCRUDTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _loggerMock = new Mock<ILogger<QuestionService>>();
        _coinServiceMock = new Mock<ICoinService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _service = new QuestionService(_context, _coinServiceMock.Object, _loggerMock.Object, _scopeFactoryMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ==================== CREATE TESTS ====================

    [Fact]
    public async Task CreateQuestionAsync_WithValidData_CreatesQuestion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        // Create admin user in database
        var adminUser = new User
        {
            Id = userId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hashedpassword",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(adminUser);
        await _context.SaveChangesAsync();
        
        var dto = new CreateQuestionDto
        {
            Title = "Test Question",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            Constraints = "1 <= n <= 10^5",
            Examples = new List<ExampleDto>
            {
                new ExampleDto
                {
                    Input = "nums = [2,7,11,15], target = 9",
                    Output = "[0,1]",
                    Explanation = "Because nums[0] + nums[1] == 9, we return [0, 1]."
                }
            },
            Hints = new List<string> { "Consider using a hash map." },
            TimeComplexityHint = "O(n)",
            SpaceComplexityHint = "O(n)"
        };

        // Act
        var result = await _service.CreateQuestionAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.Difficulty, result.Difficulty);
        Assert.Equal(dto.QuestionType, result.QuestionType);
        Assert.Equal(dto.Category, result.Category);
        Assert.Equal("Approved", result.ApprovalStatus); // Admin questions are auto-approved
        Assert.True(result.IsActive);
        Assert.Equal(userId, result.CreatedBy);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == result.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal(dto.Title, questionInDb.Title);
    }

    [Fact]
    public async Task CreateQuestionAsync_WithExamples_CreatesQuestionWithExamples()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateQuestionDto
        {
            Title = "Test Question with Examples",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            Examples = new List<ExampleDto>
            {
                new ExampleDto
                {
                    Input = "nums = [2,7,11,15], target = 9",
                    Output = "[0,1]",
                    Explanation = "Test explanation"
                }
            }
        };

        // Act
        var result = await _service.CreateQuestionAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        // Examples are stored in JSON format in the Description or Examples field
    }

    // ==================== READ TESTS ====================

    [Fact]
    public async Task GetQuestionByIdAsync_WithValidId_ReturnsQuestion()
    {
        // Arrange
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetQuestionByIdAsync(question.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question.Id, result.Id);
        Assert.Equal(question.Title, result.Title);
    }

    [Fact]
    public async Task GetQuestionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.GetQuestionByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuestionByIdAsync_WithInactiveQuestion_ReturnsNull()
    {
        // Arrange
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Inactive Question",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = false
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetQuestionByIdAsync(question.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuestionsAsync_WithNoFilter_ReturnsAllApprovedQuestions()
    {
        // Arrange
        var questions = new List<InterviewQuestion>
        {
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Question 1",
                Difficulty = "Easy",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Approved",
                IsActive = true
            },
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Question 2",
                Difficulty = "Medium",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Approved",
                IsActive = true
            },
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Pending Question",
                Difficulty = "Easy",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Pending",
                IsActive = true
            }
        };
        await _context.InterviewQuestions.AddRangeAsync(questions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetQuestionsAsync(null);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count); // Only approved questions
        Assert.All(resultList, q => Assert.Equal("Approved", q.ApprovalStatus));
    }

    [Fact]
    public async Task GetQuestionsAsync_WithDifficultyFilter_ReturnsFilteredQuestions()
    {
        // Arrange
        var questions = new List<InterviewQuestion>
        {
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Easy Question",
                Difficulty = "Easy",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Approved",
                IsActive = true
            },
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Medium Question",
                Difficulty = "Medium",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Approved",
                IsActive = true
            }
        };
        await _context.InterviewQuestions.AddRangeAsync(questions);
        await _context.SaveChangesAsync();

        var filter = new QuestionFilterDto
        {
            Difficulty = "Easy"
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("Easy", resultList[0].Difficulty);
    }

    [Fact]
    public async Task GetQuestionsAsync_WithSearchFilter_ReturnsMatchingQuestions()
    {
        // Arrange
        var questions = new List<InterviewQuestion>
        {
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Two Sum",
                Difficulty = "Easy",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Approved",
                IsActive = true
            },
            new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = "Three Sum",
                Difficulty = "Medium",
                QuestionType = "Coding",
                Category = "Arrays",
                ApprovalStatus = "Approved",
                IsActive = true
            }
        };
        await _context.InterviewQuestions.AddRangeAsync(questions);
        await _context.SaveChangesAsync();

        var filter = new QuestionFilterDto
        {
            Search = "Two"
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Contains("Two", resultList[0].Title);
    }

    // ==================== UPDATE TESTS ====================

    [Fact]
    public async Task UpdateQuestionAsync_WithValidData_UpdatesQuestion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = userId
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        var dto = new UpdateQuestionDto
        {
            Title = "Updated Title",
            Description = "Updated Description"
        };

        // Act
        var result = await _service.UpdateQuestionAsync(question.Id, dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Description", result.Description);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == question.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal("Updated Title", questionInDb.Title);
    }

    [Fact]
    public async Task UpdateQuestionAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();
        var dto = new UpdateQuestionDto
        {
            Title = "Updated Title"
        };

        // Act
        var result = await _service.UpdateQuestionAsync(invalidId, dto, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateQuestionAsync_WithUnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Original Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = ownerId
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        var dto = new UpdateQuestionDto
        {
            Title = "Updated Title"
        };

        // Act
        var result = await _service.UpdateQuestionAsync(question.Id, dto, otherUserId);

        // Assert
        Assert.Null(result);
    }

    // ==================== DELETE TESTS ====================

    [Fact]
    public async Task DeleteQuestionAsync_WithValidId_DeletesQuestion()
    {
        // Arrange
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question to Delete",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteQuestionAsync(question.Id);

        // Assert
        Assert.True(result);
        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == question.Id);
        Assert.Null(questionInDb);
    }

    [Fact]
    public async Task DeleteQuestionAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteQuestionAsync(invalidId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteQuestionAsync_DeletesAssociatedTestCases()
    {
        // Arrange
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question with Test Cases",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true
        };
        await _context.InterviewQuestions.AddAsync(question);
        
        var testCase = new QuestionTestCase
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Input = "test input",
            ExpectedOutput = "test output",
            IsHidden = false
        };
        await _context.QuestionTestCases.AddAsync(testCase);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteQuestionAsync(question.Id);

        // Assert
        Assert.True(result);
        var testCaseInDb = await _context.QuestionTestCases.FirstOrDefaultAsync(tc => tc.Id == testCase.Id);
        Assert.Null(testCaseInDb);
    }
}
