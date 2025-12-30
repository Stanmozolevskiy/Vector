using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class PeerInterviewServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ILogger<PeerInterviewService>> _loggerMock;
    private readonly PeerInterviewService _service;

    public PeerInterviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _questionServiceMock = new Mock<IQuestionService>();
        _loggerMock = new Mock<ILogger<PeerInterviewService>>();

        _service = new PeerInterviewService(
            _context,
            _questionServiceMock.Object,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ==================== SCHEDULING TESTS ====================

    [Fact]
    public async Task ScheduleInterviewSessionAsync_WithValidData_CreatesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new ScheduleInterviewDto
        {
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = await _service.ScheduleInterviewSessionAsync(userId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.InterviewType, result.InterviewType);
        Assert.Equal(dto.PracticeType, result.PracticeType);
        Assert.Equal(dto.InterviewLevel, result.InterviewLevel);
        Assert.Equal("Scheduled", result.Status);
        Assert.Equal(userId, result.UserId);

        var sessionInDb = await _context.ScheduledInterviewSessions.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Scheduled", sessionInDb.Status);
    }

    [Fact]
    public async Task ScheduleInterviewSessionAsync_WithPastDate_StillCreatesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var dto = new ScheduleInterviewDto
        {
            InterviewType = "system-design",
            PracticeType = "friend",
            InterviewLevel = "advanced",
            ScheduledStartAt = DateTime.UtcNow.AddHours(-1) // Past date
        };

        // Act
        var result = await _service.ScheduleInterviewSessionAsync(userId, dto);

        // Assert
        Assert.NotNull(result);
        var sessionInDb = await _context.ScheduledInterviewSessions.FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(sessionInDb);
    }

    [Fact]
    public async Task GetUpcomingSessionsAsync_WithFutureSessions_ReturnsOnlyFuture()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var futureSession = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(2),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pastSession = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "system-design",
            PracticeType = "peers",
            InterviewLevel = "intermediate",
            ScheduledStartAt = DateTime.UtcNow.AddHours(-1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.ScheduledInterviewSessions.AddRangeAsync(futureSession, pastSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpcomingSessionsAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal(futureSession.Id, result.First().Id);
    }

    [Fact]
    public async Task GetUpcomingSessionsAsync_ExcludesCancelledSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var activeSession = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var cancelledSession = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "system-design",
            PracticeType = "peers",
            InterviewLevel = "intermediate",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Cancelled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.ScheduledInterviewSessions.AddRangeAsync(activeSession, cancelledSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpcomingSessionsAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal(activeSession.Id, result.First().Id);
    }

    [Fact]
    public async Task GetUpcomingSessionsAsync_WithNoSessions_ReturnsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUpcomingSessionsAsync(userId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetScheduledSessionByIdAsync_WithValidId_ReturnsSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetScheduledSessionByIdAsync(session.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.Id, result.Id);
    }

    [Fact]
    public async Task GetScheduledSessionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.GetScheduledSessionByIdAsync(invalidId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetScheduledSessionByIdAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetScheduledSessionByIdAsync(session.Id, userId2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithValidId_CancelsSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, userId);

        // Assert
        Assert.True(result);
        var sessionInDb = await _context.ScheduledInterviewSessions.FindAsync(session.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Cancelled", sessionInDb.Status);
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.CancelScheduledSessionAsync(invalidId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithWrongUserId_ReturnsFalse()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, userId2);

        // Assert
        Assert.False(result);
        var sessionInDb = await _context.ScheduledInterviewSessions.FindAsync(session.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Scheduled", sessionInDb.Status); // Should still be Scheduled
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithAlreadyCancelled_StillReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Cancelled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, userId);

        // Assert
        Assert.True(result);
    }

    // ==================== MATCHING/PAIRING TESTS ====================

    [Fact]
    public async Task StartMatchingAsync_WithValidSession_CreatesMatchingRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.StartMatchingAsync(session.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.MatchingRequest);
        Assert.Equal("Pending", result.MatchingRequest.Status);

        var requestInDb = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(m => m.ScheduledSessionId == session.Id);
        Assert.NotNull(requestInDb);
        Assert.Equal("Pending", requestInDb.Status);
    }

    [Fact]
    public async Task StartMatchingAsync_WithInvalidSession_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidSessionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.StartMatchingAsync(invalidSessionId, userId));
    }

    [Fact]
    public async Task StartMatchingAsync_WithCancelledSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Cancelled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartMatchingAsync(session.Id, userId));
    }

    [Fact]
    public async Task StartMatchingAsync_WithExistingPendingRequest_ReusesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var existingRequest = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = session.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session.ScheduledStartAt,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(existingRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.StartMatchingAsync(session.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingRequest.Id, result.MatchingRequest.Id);

        var requestCount = await _context.InterviewMatchingRequests
            .CountAsync(m => m.ScheduledSessionId == session.Id);
        Assert.Equal(1, requestCount); // Should not create duplicate
    }

    [Fact]
    public async Task StartMatchingAsync_WithDifferentInterviewTypes_DoesNotMatch()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session1 = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session2 = new ScheduledInterviewSession
        {
            UserId = userId2,
            InterviewType = "system-design", // Different type
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.ScheduledInterviewSessions.AddRangeAsync(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _service.StartMatchingAsync(session1.Id, userId1);
        var result2 = await _service.StartMatchingAsync(session2.Id, userId2);

        // Assert
        Assert.False(result1.Matched);
        Assert.False(result2.Matched);
    }

    [Fact]
    public async Task StartMatchingAsync_WithDifferentPracticeTypes_DoesNotMatch()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session1 = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session2 = new ScheduledInterviewSession
        {
            UserId = userId2,
            InterviewType = "data-structures-algorithms",
            PracticeType = "friend", // Different practice type
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.ScheduledInterviewSessions.AddRangeAsync(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _service.StartMatchingAsync(session1.Id, userId1);
        var result2 = await _service.StartMatchingAsync(session2.Id, userId2);

        // Assert
        Assert.False(result1.Matched);
        Assert.False(result2.Matched);
    }

    [Fact]
    public async Task GetMatchingStatusAsync_WithNoRequest_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.GetMatchingStatusAsync(sessionId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMatchingStatusAsync_WithValidRequest_ReturnsStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var request = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = session.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session.ScheduledStartAt,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMatchingStatusAsync(session.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task GetMatchingStatusAsync_WithExpiredRequest_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var request = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = session.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session.ScheduledStartAt,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMatchingStatusAsync(session.Id, userId);

        // Assert
        Assert.Null(result);
        var requestInDb = await _context.InterviewMatchingRequests.FindAsync(request.Id);
        Assert.NotNull(requestInDb);
        Assert.Equal("Expired", requestInDb.Status);
    }

    [Fact]
    public async Task ConfirmMatchAsync_WithValidRequest_MarksAsConfirmed()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session1 = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session1);
        await _context.SaveChangesAsync();

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question 2",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = session1.Id,
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        var matchingRequest1 = new InterviewMatchingRequest
        {
            UserId = userId1,
            ScheduledSessionId = session1.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session1.ScheduledStartAt,
            Status = "Matched",
            MatchedUserId = userId2,
            LiveSessionId = liveSession.Id,
            UserConfirmed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(matchingRequest1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ConfirmMatchAsync(matchingRequest1.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.MatchingRequest.UserConfirmed);
        var requestInDb = await _context.InterviewMatchingRequests.FindAsync(matchingRequest1.Id);
        Assert.NotNull(requestInDb);
        Assert.True(requestInDb.UserConfirmed);
    }

    [Fact]
    public async Task ConfirmMatchAsync_WhenBothConfirm_CreatesLiveSession()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session1 = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session1);
        await _context.SaveChangesAsync();

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question 2",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = session1.Id,
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        var matchingRequest1 = new InterviewMatchingRequest
        {
            UserId = userId1,
            ScheduledSessionId = session1.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session1.ScheduledStartAt,
            Status = "Matched",
            MatchedUserId = userId2,
            LiveSessionId = liveSession.Id,
            UserConfirmed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var matchingRequest2 = new InterviewMatchingRequest
        {
            UserId = userId2,
            ScheduledSessionId = session1.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session1.ScheduledStartAt,
            Status = "Matched",
            MatchedUserId = userId1,
            LiveSessionId = liveSession.Id,
            UserConfirmed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddRangeAsync(matchingRequest1, matchingRequest2);
        await _context.SaveChangesAsync();

        // Act - First user confirms
        var result1 = await _service.ConfirmMatchAsync(matchingRequest1.Id, userId1);
        Assert.False(result1.Completed); // Not completed yet

        // Second user confirms
        var result2 = await _service.ConfirmMatchAsync(matchingRequest2.Id, userId2);

        // Assert
        Assert.True(result2.Completed);
        Assert.NotNull(result2.Session);
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("InProgress", sessionInDb.Status);
        Assert.NotNull(sessionInDb.StartedAt);
    }

    [Fact]
    public async Task ConfirmMatchAsync_WhenAlreadyConfirmed_ReturnsResponse()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        await _context.Users.AddAsync(user1);

        var session1 = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session1);
        await _context.SaveChangesAsync();

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Test Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddAsync(question1);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = session1.Id,
            FirstQuestionId = question1.Id,
            ActiveQuestionId = question1.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddAsync(participant1);
        await _context.SaveChangesAsync();

        var matchingRequest1 = new InterviewMatchingRequest
        {
            UserId = userId1,
            ScheduledSessionId = session1.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session1.ScheduledStartAt,
            Status = "Matched",
            LiveSessionId = liveSession.Id,
            UserConfirmed = true, // Already confirmed
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(matchingRequest1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ConfirmMatchAsync(matchingRequest1.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.MatchingRequest.UserConfirmed);
    }

    [Fact]
    public async Task ConfirmMatchAsync_WithInvalidRequest_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidRequestId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ConfirmMatchAsync(invalidRequestId, userId));
    }

    [Fact]
    public async Task ConfirmMatchAsync_WithNotMatchedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var matchingRequest = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = session.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session.ScheduledStartAt,
            Status = "Pending", // Not matched yet
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(matchingRequest);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ConfirmMatchAsync(matchingRequest.Id, userId));
    }

    [Fact]
    public async Task ExpireMatchIfNotConfirmedAsync_WithExpiredRequest_ExpiresMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var matchingRequest = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = session.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session.ScheduledStartAt,
            Status = "Matched",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow.AddSeconds(-20) // Updated more than 15 seconds ago
        };
        await _context.InterviewMatchingRequests.AddAsync(matchingRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExpireMatchIfNotConfirmedAsync(matchingRequest.Id, userId);

        // Assert
        Assert.True(result);
        var requestInDb = await _context.InterviewMatchingRequests.FindAsync(matchingRequest.Id);
        Assert.NotNull(requestInDb);
        Assert.Equal("Pending", requestInDb.Status); // Status is set back to Pending, not Expired
    }

    [Fact]
    public async Task ExpireMatchIfNotConfirmedAsync_WithConfirmedRequest_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var matchingRequest = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = session.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = session.ScheduledStartAt,
            Status = "Confirmed", // Already confirmed
            UserConfirmed = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewMatchingRequests.AddAsync(matchingRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExpireMatchIfNotConfirmedAsync(matchingRequest.Id, userId);

        // Assert
        Assert.False(result);
    }

    // ==================== QUESTION SWITCHING TESTS ====================

    [Fact]
    public async Task ChangeQuestionAsync_AsInterviewer_ChangesQuestion()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ChangeQuestionAsync(liveSession.Id, userId1, question2.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.NewActiveQuestion);
        Assert.Equal(question2.Id, result.NewActiveQuestion.Id);
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal(question2.Id, sessionInDb.ActiveQuestionId);
    }

    [Fact]
    public async Task ChangeQuestionAsync_AsInterviewee_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ChangeQuestionAsync(liveSession.Id, userId2, question2.Id));
    }

    [Fact]
    public async Task ChangeQuestionAsync_WithNullQuestionId_SelectsRandomQuestion()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        // Create multiple questions to ensure random selection works
        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding", // Maps to "data-structures-algorithms"
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question3 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 3",
            QuestionType = "Coding",
            Difficulty = "Hard",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2, question3);
        await _context.SaveChangesAsync();

        var scheduledSession = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(scheduledSession);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = scheduledSession.Id,
            FirstQuestionId = question1.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ChangeQuestionAsync(liveSession.Id, userId1, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.NewActiveQuestion);
        Assert.NotEqual(question1.Id, result.NewActiveQuestion.Id);
    }

    [Fact]
    public async Task ChangeQuestionAsync_ReplacesActiveQuestionInFirstOrSecondQuestion()
    {
        // Arrange: Session with both questions set, FirstQuestionId is active
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question3 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 3",
            QuestionType = "Coding",
            Difficulty = "Hard",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2, question3);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id, // FirstQuestionId is active
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act: Change question (should replace FirstQuestionId since it's active)
        var result = await _service.ChangeQuestionAsync(liveSession.Id, userId1, question3.Id);

        // Assert: FirstQuestionId should be replaced with question3, SecondQuestionId should remain question2
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal(question3.Id, sessionInDb.FirstQuestionId); // FirstQuestionId replaced
        Assert.Equal(question2.Id, sessionInDb.SecondQuestionId); // SecondQuestionId unchanged
        Assert.Equal(question3.Id, sessionInDb.ActiveQuestionId); // ActiveQuestionId updated
    }

    [Fact]
    public async Task ChangeQuestionAsync_ReplacesSecondQuestionWhenItIsActive()
    {
        // Arrange: Session with both questions set, SecondQuestionId is active
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question3 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 3",
            QuestionType = "Coding",
            Difficulty = "Hard",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2, question3);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question2.Id, // SecondQuestionId is active
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act: Change question (should replace SecondQuestionId since it's active)
        var result = await _service.ChangeQuestionAsync(liveSession.Id, userId1, question3.Id);

        // Assert: SecondQuestionId should be replaced with question3, FirstQuestionId should remain question1
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal(question1.Id, sessionInDb.FirstQuestionId); // FirstQuestionId unchanged
        Assert.Equal(question3.Id, sessionInDb.SecondQuestionId); // SecondQuestionId replaced
        Assert.Equal(question3.Id, sessionInDb.ActiveQuestionId); // ActiveQuestionId updated
    }

    [Fact]
    public async Task ChangeQuestionAsync_ThenSwitchRoles_StillSwitchesToOtherQuestion()
    {
        // Arrange: Session with both questions set
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question3 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 3",
            QuestionType = "Coding",
            Difficulty = "Hard",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2, question3);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id, // FirstQuestionId is active
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act 1: Change question (replaces FirstQuestionId with question3)
        await _service.ChangeQuestionAsync(liveSession.Id, userId1, question3.Id);

        // Verify FirstQuestionId was replaced
        var sessionAfterChange = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionAfterChange);
        Assert.Equal(question3.Id, sessionAfterChange.FirstQuestionId);
        Assert.Equal(question2.Id, sessionAfterChange.SecondQuestionId);
        Assert.Equal(question3.Id, sessionAfterChange.ActiveQuestionId);

        // Act 2: Switch roles (should switch to SecondQuestionId which is question2)
        var switchResult = await _service.SwitchRolesAsync(liveSession.Id, userId1);

        // Assert: After role switch, ActiveQuestionId should be question2 (SecondQuestionId)
        var sessionAfterSwitch = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionAfterSwitch);
        Assert.Equal(question2.Id, sessionAfterSwitch.ActiveQuestionId); // Should switch to SecondQuestionId
        Assert.Equal(question3.Id, sessionAfterSwitch.FirstQuestionId); // FirstQuestionId still question3
        Assert.Equal(question2.Id, sessionAfterSwitch.SecondQuestionId); // SecondQuestionId still question2
    }

    [Fact]
    public async Task ChangeQuestionAsync_WithInvalidQuestion_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddAsync(question1);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        var invalidQuestionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ChangeQuestionAsync(liveSession.Id, userId1, invalidQuestionId));
    }

    // ==================== ROLE SWITCHING TESTS ====================

    [Fact]
    public async Task SwitchRolesAsync_WithValidSession_SwapsRoles()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SwitchRolesAsync(liveSession.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Interviewee", result.YourNewRole);
        Assert.Equal("Interviewer", result.PartnerNewRole);
        
        var participant1InDb = await _context.LiveInterviewParticipants
            .FirstOrDefaultAsync(p => p.UserId == userId1 && p.LiveSessionId == liveSession.Id);
        var participant2InDb = await _context.LiveInterviewParticipants
            .FirstOrDefaultAsync(p => p.UserId == userId2 && p.LiveSessionId == liveSession.Id);
        
        Assert.NotNull(participant1InDb);
        Assert.NotNull(participant2InDb);
        Assert.Equal("Interviewee", participant1InDb.Role);
        Assert.Equal("Interviewer", participant2InDb.Role);
    }

    [Fact]
    public async Task SwitchRolesAsync_WithTwoQuestions_SwitchesActiveQuestion()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 2",
            QuestionType = "Coding",
            Difficulty = "Medium",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id, // Currently on question 1
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SwitchRolesAsync(liveSession.Id, userId1);

        // Assert
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal(question2.Id, sessionInDb.ActiveQuestionId); // Should switch to question 2
    }

    [Fact]
    public async Task SwitchRolesAsync_WithOnlyOneQuestion_DoesNotSwitchQuestion()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddAsync(question1);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            SecondQuestionId = null,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SwitchRolesAsync(liveSession.Id, userId1);

        // Assert
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal(question1.Id, sessionInDb.ActiveQuestionId); // Should stay on question 1
    }

    [Fact]
    public async Task SwitchRolesAsync_WithUserNotInSession_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.SwitchRolesAsync(liveSession.Id, userId3));
    }

    // ==================== END INTERVIEW TESTS ====================

    [Fact]
    public async Task EndInterviewAsync_WithValidSession_EndsSession()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var session = new ScheduledInterviewSession
        {
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = session.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EndInterviewAsync(liveSession.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        var sessionInDb = await _context.LiveInterviewSessions.FindAsync(liveSession.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Completed", sessionInDb.Status);
        Assert.NotNull(sessionInDb.EndedAt);
        
        var scheduledSessionInDb = await _context.ScheduledInterviewSessions.FindAsync(session.Id);
        Assert.NotNull(scheduledSessionInDb);
        Assert.Equal("Completed", scheduledSessionInDb.Status);
    }

    [Fact]
    public async Task GetPastSessionsAsync_AfterEndingInterview_ReturnsCompleteDataForBothUsers()
    {
        // Arrange: Create two users
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com", FirstName = "User", LastName = "One" };
        var user2 = new User { Id = userId2, Email = "user2@example.com", FirstName = "User", LastName = "Two" };
        await _context.Users.AddRangeAsync(user1, user2);

        // Create two scheduled sessions
        var scheduledSession1 = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(-1), // Past time
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var scheduledSession2 = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(-1), // Past time
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        await _context.ScheduledInterviewSessions.AddRangeAsync(scheduledSession1, scheduledSession2);

        // Create questions
        var question1 = new InterviewQuestion 
        { 
            Id = Guid.NewGuid(), 
            Title = "Question One", 
            QuestionType = "Coding", 
            Difficulty = "Easy", 
            IsActive = true, 
            ApprovalStatus = "Approved" 
        };
        var question2 = new InterviewQuestion 
        { 
            Id = Guid.NewGuid(), 
            Title = "Question Two", 
            QuestionType = "Coding", 
            Difficulty = "Medium", 
            IsActive = true, 
            ApprovalStatus = "Approved" 
        };
        await _context.InterviewQuestions.AddRangeAsync(question1, question2);

        // Create live session
        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = scheduledSession1.Id, // Direct link to first session
            FirstQuestionId = question1.Id,
            SecondQuestionId = question2.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        // Create participants
        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        // Create matching requests linking both scheduled sessions to the live session
        var matchingRequest1 = new InterviewMatchingRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            ScheduledSessionId = scheduledSession1.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = scheduledSession1.ScheduledStartAt,
            Status = "Confirmed",
            MatchedUserId = userId2,
            LiveSessionId = liveSession.Id,
            UserConfirmed = true,
            MatchedUserConfirmed = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        var matchingRequest2 = new InterviewMatchingRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            ScheduledSessionId = scheduledSession2.Id,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            ScheduledStartAt = scheduledSession2.ScheduledStartAt,
            Status = "Confirmed",
            MatchedUserId = userId1,
            LiveSessionId = liveSession.Id,
            UserConfirmed = true,
            MatchedUserConfirmed = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        await _context.InterviewMatchingRequests.AddRangeAsync(matchingRequest1, matchingRequest2);
        await _context.SaveChangesAsync();

        // Act: End the interview
        await _service.EndInterviewAsync(liveSession.Id, userId1);

        // Get past sessions for both users
        var pastSessionsUser1 = await _service.GetPastSessionsAsync(userId1);
        var pastSessionsUser2 = await _service.GetPastSessionsAsync(userId2);

        // Assert: Both users should have past sessions
        var session1 = pastSessionsUser1.FirstOrDefault(s => s.Id == scheduledSession1.Id);
        var session2 = pastSessionsUser2.FirstOrDefault(s => s.Id == scheduledSession2.Id);

        Assert.NotNull(session1);
        Assert.NotNull(session2);

        // Verify User 1's session has complete data
        Assert.NotNull(session1.LiveSession);
        Assert.NotNull(session1.LiveSession.FirstQuestion);
        Assert.NotNull(session1.LiveSession.SecondQuestion);
        Assert.Equal(question1.Id, session1.LiveSession.FirstQuestionId);
        Assert.Equal(question2.Id, session1.LiveSession.SecondQuestionId);
        Assert.Equal("Question One", session1.LiveSession.FirstQuestion.Title);
        Assert.Equal("Question Two", session1.LiveSession.SecondQuestion.Title);
        Assert.NotNull(session1.LiveSession.Participants);
        Assert.Equal(2, session1.LiveSession.Participants.Count());
        var interviewee1 = session1.LiveSession.Participants.FirstOrDefault(p => p.Role == "Interviewee");
        Assert.NotNull(interviewee1);
        Assert.NotNull(interviewee1.User);
        Assert.Equal(userId2, interviewee1.UserId);

        // Verify User 2's session has complete data (this is the critical test - second user should have all data)
        Assert.NotNull(session2.LiveSession);
        Assert.NotNull(session2.LiveSession.FirstQuestion);
        Assert.NotNull(session2.LiveSession.SecondQuestion);
        Assert.Equal(question1.Id, session2.LiveSession.FirstQuestionId);
        Assert.Equal(question2.Id, session2.LiveSession.SecondQuestionId);
        Assert.Equal("Question One", session2.LiveSession.FirstQuestion.Title);
        Assert.Equal("Question Two", session2.LiveSession.SecondQuestion.Title);
        Assert.NotNull(session2.LiveSession.Participants);
        Assert.Equal(2, session2.LiveSession.Participants.Count());
        var interviewer2 = session2.LiveSession.Participants.FirstOrDefault(p => p.Role == "Interviewer");
        Assert.NotNull(interviewer2);
        Assert.NotNull(interviewer2.User);
        Assert.Equal(userId1, interviewer2.UserId);

        // Verify both scheduled sessions are marked as Completed
        var scheduled1InDb = await _context.ScheduledInterviewSessions.FindAsync(scheduledSession1.Id);
        var scheduled2InDb = await _context.ScheduledInterviewSessions.FindAsync(scheduledSession2.Id);
        Assert.NotNull(scheduled1InDb);
        Assert.NotNull(scheduled2InDb);
        Assert.Equal("Completed", scheduled1InDb.Status);
        Assert.Equal("Completed", scheduled2InDb.Status);
    }

    [Fact]
    public async Task EndInterviewAsync_WithUserNotInSession_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.EndInterviewAsync(liveSession.Id, userId3));
    }

    [Fact]
    public async Task EndInterviewAsync_WithCompletedSession_StillWorks()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EndInterviewAsync(liveSession.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
    }

    // ==================== FEEDBACK TESTS ====================

    [Fact]
    public async Task SubmitFeedbackAsync_WithValidData_CreatesFeedback()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        var dto = new SubmitFeedbackDto
        {
            LiveSessionId = liveSession.Id,
            RevieweeId = userId2,
            ProblemSolvingRating = 4,
            ProblemSolvingDescription = "Good problem solving",
            CodingSkillsRating = 5,
            CommunicationRating = 4,
            ThingsDidWell = "Clear communication",
            AreasForImprovement = "Could improve time complexity",
            InterviewerPerformanceRating = 5
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(userId1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId1, result.ReviewerId);
        Assert.Equal(userId2, result.RevieweeId);
        Assert.Equal(4, result.ProblemSolvingRating);
        Assert.Equal(5, result.CodingSkillsRating);

        var feedbackInDb = await _context.InterviewFeedbacks
            .FirstOrDefaultAsync(f => f.ReviewerId == userId1 && f.RevieweeId == userId2);
        Assert.NotNull(feedbackInDb);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_WithExistingFeedback_UpdatesFeedback()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        var existingFeedback = new InterviewFeedback
        {
            LiveSessionId = liveSession.Id,
            ReviewerId = userId1,
            RevieweeId = userId2,
            ProblemSolvingRating = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewFeedbacks.AddAsync(existingFeedback);
        await _context.SaveChangesAsync();

        var dto = new SubmitFeedbackDto
        {
            LiveSessionId = liveSession.Id,
            RevieweeId = userId2,
            ProblemSolvingRating = 5, // Updated rating
            CodingSkillsRating = 5
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(userId1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.ProblemSolvingRating);
        
        var feedbackInDb = await _context.InterviewFeedbacks.FindAsync(existingFeedback.Id);
        Assert.NotNull(feedbackInDb);
        Assert.Equal(5, feedbackInDb.ProblemSolvingRating);
        
        var feedbackCount = await _context.InterviewFeedbacks
            .CountAsync(f => f.LiveSessionId == liveSession.Id && f.ReviewerId == userId1);
        Assert.Equal(1, feedbackCount); // Should still be 1, not 2
    }

    [Fact]
    public async Task SubmitFeedbackAsync_WithSelfAsReviewee_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        var dto = new SubmitFeedbackDto
        {
            LiveSessionId = liveSession.Id,
            RevieweeId = userId1, // Trying to review self
            ProblemSolvingRating = 5
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitFeedbackAsync(userId1, dto));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_WithUserNotInSession_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        var dto = new SubmitFeedbackDto
        {
            LiveSessionId = liveSession.Id,
            RevieweeId = userId2,
            ProblemSolvingRating = 5
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.SubmitFeedbackAsync(userId3, dto));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_WithInvalidReviewee_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        var dto = new SubmitFeedbackDto
        {
            LiveSessionId = liveSession.Id,
            RevieweeId = userId3, // User3 is not in the session
            ProblemSolvingRating = 5
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitFeedbackAsync(userId1, dto));
    }

    [Fact]
    public async Task GetFeedbackForSessionAsync_WithNoFeedback_ReturnsEmptyList()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFeedbackForSessionAsync(liveSession.Id, userId1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeedbackForSessionAsync_WithFeedback_ReturnsFeedbackAboutUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        // Feedback about userId1 (userId2 reviews userId1)
        var feedback = new InterviewFeedback
        {
            LiveSessionId = liveSession.Id,
            ReviewerId = userId2,
            RevieweeId = userId1,
            ProblemSolvingRating = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewFeedbacks.AddAsync(feedback);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFeedbackForSessionAsync(liveSession.Id, userId1);

        // Assert
        Assert.Single(result);
        Assert.Equal(feedback.Id, result.First().Id);
        Assert.Equal(userId2, result.First().ReviewerId);
        Assert.Equal(userId1, result.First().RevieweeId);
    }

    [Fact]
    public async Task GetFeedbackForSessionAsync_WithUserNotInSession_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetFeedbackForSessionAsync(liveSession.Id, userId3));
    }

    [Fact]
    public async Task GetFeedbackAsync_WithValidId_ReturnsFeedback()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        var feedback = new InterviewFeedback
        {
            LiveSessionId = liveSession.Id,
            ReviewerId = userId1,
            RevieweeId = userId2,
            ProblemSolvingRating = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewFeedbacks.AddAsync(feedback);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFeedbackAsync(feedback.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(feedback.Id, result.Id);
    }

    [Fact]
    public async Task GetFeedbackAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.GetFeedbackAsync(invalidId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFeedbackAsync_WithUserNotInSession_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);

        var feedback = new InterviewFeedback
        {
            LiveSessionId = liveSession.Id,
            ReviewerId = userId1,
            RevieweeId = userId2,
            ProblemSolvingRating = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewFeedbacks.AddAsync(feedback);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetFeedbackAsync(feedback.Id, userId3));
    }

    [Fact]
    public async Task GetLiveSessionByIdAsync_WithValidSession_ReturnsSession()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        await _context.Users.AddRangeAsync(user1, user2);

        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = "Question 1",
            QuestionType = "Coding",
            Difficulty = "Easy",
            IsActive = true,
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddAsync(question1);
        await _context.SaveChangesAsync();

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            FirstQuestionId = question1.Id,
            ActiveQuestionId = question1.Id,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLiveSessionByIdAsync(liveSession.Id, userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(liveSession.Id, result.Id);
        Assert.Equal("InProgress", result.Status);
    }

    [Fact]
    public async Task GetLiveSessionByIdAsync_WithUserNotInSession_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new User { Id = userId1, Email = "user1@example.com" };
        var user2 = new User { Id = userId2, Email = "user2@example.com" };
        var user3 = new User { Id = userId3, Email = "user3@example.com" };
        await _context.Users.AddRangeAsync(user1, user2, user3);

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var participant1 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId1,
            Role = "Interviewer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var participant2 = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = userId2,
            Role = "Interviewee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.LiveInterviewParticipants.AddRangeAsync(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetLiveSessionByIdAsync(liveSession.Id, userId3));
    }
}
