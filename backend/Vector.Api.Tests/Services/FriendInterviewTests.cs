using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class FriendInterviewTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ICoinService> _coinServiceMock;
    private readonly Mock<ILogger<PeerInterviewService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly PeerInterviewService _service;

    public FriendInterviewTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _questionServiceMock = new Mock<IQuestionService>();
        _coinServiceMock = new Mock<ICoinService>();
        _loggerMock = new Mock<ILogger<PeerInterviewService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _service = new PeerInterviewService(
            _context,
            _questionServiceMock.Object,
            _coinServiceMock.Object,
            _loggerMock.Object,
            _serviceProviderMock.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateFriendInterviewAsync_WithCodingType_CreatesLiveSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "creator@test.com",
            FirstName = "Creator",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Two Sum",
            Description = "Find two numbers that add up to target",
            Difficulty = "easy",
            QuestionType = "Coding",
            Category = "Arrays",
            IsActive = true,
            ApprovalStatus = "Approved",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();

        var dto = new CreateFriendInterviewDto
        {
            InterviewType = "data-structures-algorithms",
            InterviewLevel = "easy",
            PartnerEmail = "partner@test.com"
        };

        // Act
        var result = await _service.CreateFriendInterviewAsync(userId, dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.LiveSessionId);
        Assert.NotEqual(Guid.Empty, result.CreatorScheduledSessionId);
        Assert.Equal("data-structures-algorithms", result.InterviewType);
        Assert.NotNull(result.ActiveQuestionId);

        var liveSession = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == result.LiveSessionId);
        Assert.NotNull(liveSession);
        Assert.Single(liveSession.Participants); // Only creator initially
        Assert.Equal(userId, liveSession.Participants.First().UserId);
    }

    [Fact]
    public async Task CreateFriendInterviewAsync_WithSystemDesignType_CreatesSharedExcalidrawRoom()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "creator@test.com",
            FirstName = "Creator",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new CreateFriendInterviewDto
        {
            InterviewType = "system-design",
            InterviewLevel = "medium",
            PartnerEmail = "partner@test.com"
        };

        // Act
        var result = await _service.CreateFriendInterviewAsync(userId, dto);

        // Assert
        Assert.Equal("system-design", result.InterviewType);
        Assert.Null(result.ActiveQuestionId); // System design doesn't use question IDs

        var liveSession = await _context.LiveInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == result.LiveSessionId);
        Assert.NotNull(liveSession);
        Assert.Equal(liveSession.InterviewerRoomId, liveSession.IntervieweeRoomId); // Shared room
        Assert.NotNull(liveSession.InterviewerRoomId);
    }

    [Fact]
    public async Task JoinFriendInterviewAsync_AddsSecondParticipant()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        
        var creator = new User
        {
            Id = creatorId,
            Email = "creator@test.com",
            FirstName = "Creator",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        var joiner = new User
        {
            Id = joinerId,
            Email = "joiner@test.com",
            FirstName = "Joiner",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(creator);
        await _context.Users.AddAsync(joiner);
        await _context.SaveChangesAsync();

        var scheduledSession = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = creatorId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "friend",
            InterviewLevel = "easy",
            ScheduledStartAt = DateTime.UtcNow,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(scheduledSession);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = scheduledSession.Id,
            FirstQuestionId = Guid.NewGuid(),
            SecondQuestionId = Guid.NewGuid(),
            ActiveQuestionId = Guid.NewGuid(),
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSession.Id,
            UserId = creatorId,
            Role = "Interviewer",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddAsync(participant1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.JoinFriendInterviewAsync(liveSession.Id, joinerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(liveSession.Id, result.Id);

        var updatedSession = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == liveSession.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(2, updatedSession.Participants.Count); // Both participants
        Assert.Contains(updatedSession.Participants, p => p.UserId == creatorId);
        Assert.Contains(updatedSession.Participants, p => p.UserId == joinerId);
    }

    [Fact]
    public async Task JoinFriendInterviewAsync_WhenAlreadyJoined_DoesNotDuplicate()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();

        var creator = new User
        {
            Id = creatorId,
            Email = "creator@test.com",
            FirstName = "Creator",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        var joiner = new User
        {
            Id = joinerId,
            Email = "joiner@test.com",
            FirstName = "Joiner",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(creator);
        await _context.Users.AddAsync(joiner);
        await _context.SaveChangesAsync();

        var scheduledSession = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = creatorId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "friend",
            InterviewLevel = "easy",
            ScheduledStartAt = DateTime.UtcNow,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(scheduledSession);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = scheduledSession.Id,
            FirstQuestionId = Guid.NewGuid(),
            SecondQuestionId = Guid.NewGuid(),
            ActiveQuestionId = Guid.NewGuid(),
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSession.Id,
            UserId = creatorId,
            Role = "Interviewer",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSession.Id,
            UserId = joinerId,
            Role = "Interviewee",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act - try to join again
        var result = await _service.JoinFriendInterviewAsync(liveSession.Id, joinerId);

        // Assert
        Assert.NotNull(result);
        
        var updatedSession = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == liveSession.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(2, updatedSession.Participants.Count); // Still only 2 participants
    }

    [Fact]
    public async Task CreateFriendInterviewAsync_WithInvalidUser_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var dto = new CreateFriendInterviewDto
        {
            InterviewType = "data-structures-algorithms",
            InterviewLevel = "easy",
            PartnerEmail = "partner@test.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFriendInterviewAsync(nonExistentUserId, dto)
        );
    }

    [Fact]
    public async Task CreateFriendInterviewAsync_WithEmptyInterviewType_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            Role = "User",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new CreateFriendInterviewDto
        {
            InterviewType = "",
            InterviewLevel = "easy",
            PartnerEmail = "partner@test.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFriendInterviewAsync(userId, dto)
        );
    }
}
