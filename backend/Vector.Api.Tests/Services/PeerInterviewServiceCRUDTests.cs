using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class PeerInterviewServiceCRUDTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ILogger<PeerInterviewService>> _loggerMock;
    private readonly PeerInterviewService _service;

    public PeerInterviewServiceCRUDTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _questionServiceMock = new Mock<IQuestionService>();
        _loggerMock = new Mock<ILogger<PeerInterviewService>>();
        _service = new PeerInterviewService(_context, _questionServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ==================== CREATE TESTS ====================

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
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
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

        var sessionInDb = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == result.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Scheduled", sessionInDb.Status);
    }

    [Fact]
    public async Task ScheduleInterviewSessionAsync_WithPastTime_ReturnsNull()
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
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(-1) // Past time
        };

        // Act
        var result = await _service.ScheduleInterviewSessionAsync(userId, dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ScheduleInterviewSessionAsync_WithInvalidUserId_ThrowsException()
    {
        // Arrange
        var invalidUserId = Guid.NewGuid();
        var dto = new ScheduleInterviewDto
        {
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ScheduleInterviewSessionAsync(invalidUserId, dto));
    }

    // ==================== READ TESTS ====================

    [Fact]
    public async Task GetScheduledSessionByIdAsync_WithValidId_ReturnsSession()
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

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled"
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetScheduledSessionByIdAsync(session.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.Id, result.Id);
        Assert.Equal(session.Status, result.Status);
    }

    [Fact]
    public async Task GetScheduledSessionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidSessionId = Guid.NewGuid();

        // Act
        var result = await _service.GetScheduledSessionByIdAsync(invalidSessionId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetScheduledSessionByIdAsync_WithDifferentUserId_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var user = new User
        {
            Id = ownerId,
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User"
        };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled"
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetScheduledSessionByIdAsync(session.Id, otherUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUpcomingSessionsAsync_WithValidUserId_ReturnsUpcomingSessions()
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

        var sessions = new List<ScheduledInterviewSession>
        {
            new ScheduledInterviewSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InterviewType = "Peer",
                PracticeType = "Mock",
                InterviewLevel = "Beginner",
                ScheduledStartAt = DateTime.UtcNow.AddHours(1),
                Status = "Scheduled"
            },
            new ScheduledInterviewSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InterviewType = "Peer",
                PracticeType = "Mock",
                InterviewLevel = "Intermediate",
                ScheduledStartAt = DateTime.UtcNow.AddHours(2),
                Status = "Scheduled"
            },
            new ScheduledInterviewSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InterviewType = "Peer",
                PracticeType = "Mock",
                InterviewLevel = "Advanced",
                ScheduledStartAt = DateTime.UtcNow.AddHours(-1), // Past session
                Status = "Completed"
            }
        };
        await _context.ScheduledInterviewSessions.AddRangeAsync(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpcomingSessionsAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count); // Only upcoming sessions
        Assert.All(resultList, s => Assert.True(s.ScheduledStartAt > DateTime.UtcNow));
    }

    [Fact]
    public async Task GetPastSessionsAsync_WithValidUserId_ReturnsPastSessions()
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

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddHours(-2),
            EndedAt = DateTime.UtcNow.AddHours(-1)
        };
        await _context.LiveInterviewSessions.AddAsync(liveSession);

        var sessions = new List<ScheduledInterviewSession>
        {
            new ScheduledInterviewSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InterviewType = "Peer",
                PracticeType = "Mock",
                InterviewLevel = "Beginner",
                ScheduledStartAt = DateTime.UtcNow.AddHours(-2),
                Status = "Completed",
                LiveSession = liveSession
            },
            new ScheduledInterviewSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InterviewType = "Peer",
                PracticeType = "Mock",
                InterviewLevel = "Intermediate",
                ScheduledStartAt = DateTime.UtcNow.AddHours(1), // Future session
                Status = "Scheduled"
            }
        };
        await _context.ScheduledInterviewSessions.AddRangeAsync(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPastSessionsAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList); // Only completed past sessions
        Assert.Equal("Completed", resultList[0].Status);
    }

    [Fact]
    public async Task GetPastSessionsAsync_ExcludesCancelledSessions()
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

        var sessions = new List<ScheduledInterviewSession>
        {
            new ScheduledInterviewSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InterviewType = "Peer",
                PracticeType = "Mock",
                InterviewLevel = "Beginner",
                ScheduledStartAt = DateTime.UtcNow.AddHours(-2),
                Status = "Cancelled"
            }
        };
        await _context.ScheduledInterviewSessions.AddRangeAsync(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPastSessionsAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Empty(resultList); // Cancelled sessions should not appear
    }

    // ==================== UPDATE TESTS ====================

    [Fact]
    public async Task CancelScheduledSessionAsync_WithValidId_CancelsSession()
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

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled"
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, userId);

        // Assert
        Assert.True(result);
        var sessionInDb = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == session.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Cancelled", sessionInDb.Status);
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidSessionId = Guid.NewGuid();

        // Act
        var result = await _service.CancelScheduledSessionAsync(invalidSessionId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithDifferentUserId_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var user = new User
        {
            Id = ownerId,
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "User"
        };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled"
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, otherUserId);

        // Assert
        Assert.False(result);
        var sessionInDb = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == session.Id);
        Assert.NotNull(sessionInDb);
        Assert.Equal("Scheduled", sessionInDb.Status); // Status unchanged
    }

    [Fact]
    public async Task CancelScheduledSessionAsync_WithAlreadyCompletedSession_ReturnsFalse()
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

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(-1),
            Status = "Completed"
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, userId);

        // Assert
        Assert.False(result);
    }

    // ==================== DELETE TESTS ====================
    // Note: In this system, cancellation is effectively a delete operation
    // There's no explicit delete method, so we test cancellation as delete

    [Fact]
    public async Task CancelScheduledSessionAsync_EffectivelyDeletesSession()
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

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled"
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelScheduledSessionAsync(session.Id, userId);

        // Assert
        Assert.True(result);
        // Session should not appear in upcoming sessions
        var upcomingSessions = await _service.GetUpcomingSessionsAsync(userId);
        var upcomingList = upcomingSessions.ToList();
        Assert.DoesNotContain(upcomingList, s => s.Id == session.Id);
    }
}
