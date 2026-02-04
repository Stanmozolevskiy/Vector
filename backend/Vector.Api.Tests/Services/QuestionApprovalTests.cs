using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Vector.Api.Data;
using Vector.Api.DTOs.Question;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class QuestionApprovalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<QuestionService>> _loggerMock;
    private readonly QuestionService _service;

    public QuestionApprovalTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _loggerMock = new Mock<ILogger<QuestionService>>();
        _service = new QuestionService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateQuestionAsync_AsRegularUser_CreatesPendingQuestion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "Regular",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User", // Regular user, not admin
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new CreateQuestionDto
        {
            Title = "Test Question",
            Description = "Test Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            Tags = new List<string> { "array", "sorting" }
        };

        // Act
        var result = await _service.CreateQuestionAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.ApprovalStatus);
        Assert.False(result.IsActive); // Regular user questions are inactive until approved
        Assert.Equal(userId, result.CreatedBy);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == result.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal("Pending", questionInDb.ApprovalStatus);
        Assert.False(questionInDb.IsActive);
    }

    [Fact]
    public async Task CreateQuestionAsync_AsAdmin_CreatesApprovedQuestion()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

        var dto = new CreateQuestionDto
        {
            Title = "Admin Question",
            Description = "Admin Description",
            Difficulty = "Medium",
            QuestionType = "System Design",
            Category = "Distributed Systems"
        };

        // Act
        var result = await _service.CreateQuestionAsync(dto, adminId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Approved", result.ApprovalStatus);
        Assert.True(result.IsActive); // Admin questions are immediately active
        Assert.Equal(adminId, result.CreatedBy);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == result.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal("Approved", questionInDb.ApprovalStatus);
        Assert.True(questionInDb.IsActive);
    }

    [Fact]
    public async Task ApproveQuestionAsync_ChangesStatusToApproved()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var admin = new User
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Pending Question",
            Description = "Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Pending",
            IsActive = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ApproveQuestionAsync(question.Id, adminId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Approved", result.ApprovalStatus);
        Assert.True(result.IsActive);
        Assert.Equal(adminId, result.ApprovedBy);
        Assert.NotNull(result.ApprovedAt);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == question.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal("Approved", questionInDb.ApprovalStatus);
        Assert.True(questionInDb.IsActive);
    }

    [Fact]
    public async Task RejectQuestionAsync_ChangesStatusToRejected()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Pending Question",
            Description = "Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Pending",
            IsActive = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        var rejectionReason = "Does not meet quality standards";

        // Act
        var result = await _service.RejectQuestionAsync(question.Id, adminId, rejectionReason);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Rejected", result.ApprovalStatus);
        Assert.False(result.IsActive);
        Assert.Equal(rejectionReason, result.RejectionReason);
        Assert.Equal(adminId, result.ApprovedBy); // ApprovedBy is used for both approval and rejection
        Assert.NotNull(result.ApprovedAt);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == question.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal("Rejected", questionInDb.ApprovalStatus);
        Assert.False(questionInDb.IsActive);
    }

    [Fact]
    public async Task GetPendingQuestionsAsync_ReturnsOnlyPendingQuestions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var pendingQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Pending Question",
            Description = "Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Pending",
            IsActive = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var approvedQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Approved Question",
            Description = "Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rejectedQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Rejected Question",
            Description = "Description",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Rejected",
            IsActive = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.InterviewQuestions.AddRangeAsync(pendingQuestion, approvedQuestion, rejectedQuestion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingQuestionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, q => Assert.Equal("Pending", q.ApprovalStatus));
    }

    [Fact]
    public async Task UpdateQuestionAsync_AsAdmin_UpdatesApprovedQuestion()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

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
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateQuestionDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Difficulty = "Medium",
            QuestionType = "Coding",
            Category = "Dynamic Programming"
        };

        // Act
        var result = await _service.UpdateQuestionAsync(question.Id, updateDto, adminId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal("Medium", result.Difficulty);
        Assert.Equal("Dynamic Programming", result.Category);
        Assert.Equal("Approved", result.ApprovalStatus); // Remains approved
        Assert.True(result.IsActive);

        var questionInDb = await _context.InterviewQuestions.FirstOrDefaultAsync(q => q.Id == question.Id);
        Assert.NotNull(questionInDb);
        Assert.Equal("Updated Title", questionInDb.Title);
    }

    [Fact]
    public async Task CreateQuestionAsync_ProductManagementType_CreatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new CreateQuestionDto
        {
            Title = "Product Roadmap Planning",
            Description = "How would you prioritize features for a new product?",
            Difficulty = "Medium", // PM questions may have difficulty
            QuestionType = "Product Management",
            Category = "Strategy"
        };

        // Act
        var result = await _service.CreateQuestionAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Product Management", result.QuestionType);
        Assert.Equal("Strategy", result.Category);
        Assert.Equal("Pending", result.ApprovalStatus);
    }

    [Fact]
    public async Task CreateQuestionAsync_BehavioralType_CreatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new CreateQuestionDto
        {
            Title = "Conflict Resolution",
            Description = "Tell me about a time when you had a conflict with a team member.",
            Difficulty = "Medium",
            QuestionType = "Behavioral",
            Category = "Teamwork"
        };

        // Act
        var result = await _service.CreateQuestionAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Behavioral", result.QuestionType);
        Assert.Equal("Teamwork", result.Category);
        Assert.Equal("Pending", result.ApprovalStatus);
    }

    [Fact]
    public async Task GetQuestionsAsync_WithProductManagementFilter_ReturnsCorrectQuestions()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

        var pmQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Product Strategy",
            Description = "How would you launch a new product?",
            Difficulty = "Medium",
            QuestionType = "Product Management",
            Category = "Strategy",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var codingQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Two Sum",
            Description = "Find two numbers",
            Difficulty = "Easy",
            QuestionType = "Coding",
            Category = "Arrays",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.InterviewQuestions.AddRangeAsync(pmQuestion, codingQuestion);
        await _context.SaveChangesAsync();

        var filter = new QuestionFilterDto
        {
            QuestionType = "Product Management",
            IsActive = true
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, q => Assert.Equal("Product Management", q.QuestionType));
    }

    [Fact]
    public async Task GetQuestionsAsync_WithBehavioralFilter_ReturnsCorrectQuestions()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash",
            Role = "Admin",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

        var behavioralQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Leadership Example",
            Description = "Tell me about a time you led a team.",
            Difficulty = "Medium",
            QuestionType = "Behavioral",
            Category = "Leadership",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var sqlQuestion = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "SQL Query",
            Description = "Write a complex join",
            Difficulty = "Medium",
            QuestionType = "SQL",
            Category = "Databases",
            ApprovalStatus = "Approved",
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.InterviewQuestions.AddRangeAsync(behavioralQuestion, sqlQuestion);
        await _context.SaveChangesAsync();

        var filter = new QuestionFilterDto
        {
            QuestionType = "Behavioral",
            IsActive = true
        };

        // Act
        var result = await _service.GetQuestionsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, q => Assert.Equal("Behavioral", q.QuestionType));
    }
}
