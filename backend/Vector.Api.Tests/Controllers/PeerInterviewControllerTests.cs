using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.Data;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class PeerInterviewControllerTests
{
    private readonly Mock<IPeerInterviewService> _peerInterviewServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<PeerInterviewController>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly PeerInterviewController _controller;

    public PeerInterviewControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _peerInterviewServiceMock = new Mock<IPeerInterviewService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<PeerInterviewController>>();

        _controller = new PeerInterviewController(
            _peerInterviewServiceMock.Object,
            _emailServiceMock.Object,
            _context,
            _loggerMock.Object
        );

        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                FirstName = "User",
                LastName = "One",
                Role = "student",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "user2@test.com",
                FirstName = "User",
                LastName = "Two",
                Role = "student",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Users.AddRange(users);
        _context.SaveChanges();
    }

    private void SetupControllerWithUser(Guid userId, string role = "student")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupControllerWithoutUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }

    #region CreateSession Tests

    [Fact]
    public async Task CreateSession_WithValidData_ReturnsCreated()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var session = new PeerInterviewSession
        {
            Id = Guid.NewGuid(),
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(session);

        var request = new CreateSessionRequest
        {
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner"
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.CreateSessionAsync(
            request.InterviewerId,
            request.IntervieweeId,
            request.QuestionId,
            request.ScheduledTime,
            request.Duration ?? 45,
            request.InterviewType,
            request.PracticeType,
            request.InterviewLevel
        ), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var request = new CreateSessionRequest
        {
            InterviewerId = Guid.NewGuid(), // Different user
            IntervieweeId = Guid.NewGuid(), // Different user
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = "beginner"
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    [Fact]
    public async Task CreateSession_WithUserAsInterviewer_AllowsCreation()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var session = new PeerInterviewSession
        {
            Id = Guid.NewGuid(),
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(session);

        var request = new CreateSessionRequest
        {
            InterviewerId = user.Id, // User is interviewer
            IntervieweeId = _context.Users.Skip(1).First().Id,
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.CreateSessionAsync(
            user.Id,
            request.IntervieweeId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WithUserAsInterviewee_AllowsCreation()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var session = new PeerInterviewSession
        {
            Id = Guid.NewGuid(),
            InterviewerId = _context.Users.Skip(1).First().Id,
            IntervieweeId = user.Id, // User is interviewee
            Status = "Scheduled",
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(session);

        var request = new CreateSessionRequest
        {
            InterviewerId = _context.Users.Skip(1).First().Id,
            IntervieweeId = user.Id, // User is interviewee
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.CreateSessionAsync(
            request.InterviewerId,
            user.Id,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ThrowsAsync(new Exception("Database error"));

        var request = new CreateSessionRequest
        {
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WithNullDuration_UsesDefault45()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var session = new PeerInterviewSession
        {
            Id = Guid.NewGuid(),
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 45, // Default
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            45, // Should use default 45
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(session);

        var request = new CreateSessionRequest
        {
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = null // No duration specified
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            45, // Should use default
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public async Task CreateSession_WithAllOptionalFields_IncludesAllFields()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var questionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = Guid.NewGuid(),
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            QuestionId = questionId,
            Status = "Scheduled",
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 60,
            InterviewType = "system-design",
            PracticeType = "friend",
            InterviewLevel = "advanced",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(session);

        var request = new CreateSessionRequest
        {
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            QuestionId = questionId,
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Duration = 60,
            InterviewType = "system-design",
            PracticeType = "friend",
            InterviewLevel = "advanced"
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.CreateSessionAsync(
            user.Id,
            request.IntervieweeId,
            questionId,
            request.ScheduledTime,
            60,
            "system-design",
            "friend",
            "advanced"
        ), Times.Once);
    }

    #endregion

    #region GetMySessions Tests

    [Fact]
    public async Task GetMySessions_WithValidUser_ReturnsSessions()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var sessions = new List<PeerInterviewSession>
        {
            new PeerInterviewSession
            {
                Id = Guid.NewGuid(),
                InterviewerId = user.Id,
                IntervieweeId = _context.Users.Skip(1).First().Id,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _peerInterviewServiceMock.Setup(x => x.GetUserSessionsAsync(user.Id, null))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetMySessions(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<List<PeerInterviewSession>>(okResult.Value);
        Assert.Single(returnedSessions);
        _peerInterviewServiceMock.Verify(x => x.GetUserSessionsAsync(user.Id, null), Times.Once);
    }

    [Fact]
    public async Task GetMySessions_WithStatusFilter_ReturnsFilteredSessions()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var sessions = new List<PeerInterviewSession>
        {
            new PeerInterviewSession
            {
                Id = Guid.NewGuid(),
                InterviewerId = user.Id,
                IntervieweeId = _context.Users.Skip(1).First().Id,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _peerInterviewServiceMock.Setup(x => x.GetUserSessionsAsync(user.Id, "Completed"))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetMySessions("Completed");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<List<PeerInterviewSession>>(okResult.Value);
        Assert.Single(returnedSessions);
        Assert.All(returnedSessions, s => Assert.Equal("Completed", s.Status));
        _peerInterviewServiceMock.Verify(x => x.GetUserSessionsAsync(user.Id, "Completed"), Times.Once);
    }

    [Fact]
    public async Task GetMySessions_WithNoSessions_ReturnsEmptyList()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.GetUserSessionsAsync(user.Id, null))
            .ReturnsAsync(new List<PeerInterviewSession>());

        // Act
        var result = await _controller.GetMySessions(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<List<PeerInterviewSession>>(okResult.Value);
        Assert.Empty(returnedSessions);
    }

    [Fact]
    public async Task GetMySessions_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.GetUserSessionsAsync(user.Id, null))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMySessions(null);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetSession Tests

    [Fact]
    public async Task GetSession_WithValidIdAndAccess_ReturnsSession()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSession = Assert.IsType<PeerInterviewSession>(okResult.Value);
        Assert.Equal(sessionId, returnedSession.Id);
    }

    [Fact]
    public async Task GetSession_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync((PeerInterviewSession?)null);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSession_WithUnauthorizedAccess_ReturnsForbid()
    {
        // Arrange
        var user = _context.Users.First();
        var otherUser = _context.Users.Skip(1).First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = Guid.NewGuid(), // Different user
            IntervieweeId = Guid.NewGuid(), // Different user
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetSession_WithUserAsInterviewer_AllowsAccess()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = user.Id, // User is interviewer
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSession_WithUserAsInterviewee_AllowsAccess()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = _context.Users.Skip(1).First().Id,
            IntervieweeId = user.Id, // User is interviewee
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region UpdateSessionStatus Tests

    [Fact]
    public async Task UpdateSessionStatus_WithValidData_UpdatesStatus()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updatedSession = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);
        _peerInterviewServiceMock.Setup(x => x.UpdateSessionStatusAsync(sessionId, "InProgress"))
            .ReturnsAsync(updatedSession);

        var request = new UpdateStatusRequest { Status = "InProgress" };

        // Act
        var result = await _controller.UpdateSessionStatus(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSession = Assert.IsType<PeerInterviewSession>(okResult.Value);
        Assert.Equal("InProgress", returnedSession.Status);
    }

    [Fact]
    public async Task UpdateSessionStatus_WithInvalidSessionId_ReturnsNotFound()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync((PeerInterviewSession?)null);

        var request = new UpdateStatusRequest { Status = "Completed" };

        // Act
        var result = await _controller.UpdateSessionStatus(sessionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateSessionStatus_WithUnauthorizedAccess_ReturnsForbid()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = Guid.NewGuid(), // Different user
            IntervieweeId = Guid.NewGuid(), // Different user
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);

        var request = new UpdateStatusRequest { Status = "Completed" };

        // Act
        var result = await _controller.UpdateSessionStatus(sessionId, request);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result.Result);
        _peerInterviewServiceMock.Verify(x => x.UpdateSessionStatusAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    [Fact]
    public async Task UpdateSessionStatus_WithKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        var session = new PeerInterviewSession
        {
            Id = sessionId,
            InterviewerId = user.Id,
            IntervieweeId = _context.Users.Skip(1).First().Id,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(session);
        _peerInterviewServiceMock.Setup(x => x.UpdateSessionStatusAsync(sessionId, "Completed"))
            .ThrowsAsync(new KeyNotFoundException());

        var request = new UpdateStatusRequest { Status = "Completed" };

        // Act
        var result = await _controller.UpdateSessionStatus(sessionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region CancelSession Tests

    [Fact]
    public async Task CancelSession_WithValidScheduledSession_CancelsSession()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        _peerInterviewServiceMock.Setup(x => x.CancelSessionAsync(sessionId, user.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _peerInterviewServiceMock.Verify(x => x.CancelSessionAsync(sessionId, user.Id), Times.Once);
    }

    [Fact]
    public async Task CancelSession_WithInvalidSession_ReturnsBadRequest()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        _peerInterviewServiceMock.Setup(x => x.CancelSessionAsync(sessionId, user.Id))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelSession(sessionId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CancelSession_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        _peerInterviewServiceMock.Setup(x => x.CancelSessionAsync(sessionId, user.Id))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CancelSession(sessionId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CancelSession_WithInProgressSession_CancelsSession()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);
        var sessionId = Guid.NewGuid();

        _peerInterviewServiceMock.Setup(x => x.CancelSessionAsync(sessionId, user.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _peerInterviewServiceMock.Verify(x => x.CancelSessionAsync(sessionId, user.Id), Times.Once);
    }

    #endregion

    #region FindMatch Tests

    [Fact]
    public async Task FindMatch_WithAvailablePeer_ReturnsMatch()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var match = new PeerInterviewMatch
        {
            Id = Guid.NewGuid(),
            UserId = _context.Users.Skip(1).First().Id,
            PreferredDifficulty = "Easy",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.FindMatchAsync(
            user.Id,
            It.IsAny<string>(),
            It.IsAny<List<string>>()
        )).ReturnsAsync(match);

        var request = new FindMatchRequest
        {
            PreferredDifficulty = "Easy",
            PreferredCategories = new List<string> { "Arrays" }
        };

        // Act
        var result = await _controller.FindMatch(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMatch = Assert.IsType<PeerInterviewMatch>(okResult.Value);
        Assert.Equal(match.Id, returnedMatch.Id);
    }

    [Fact]
    public async Task FindMatch_WithNoAvailablePeer_ReturnsNotFound()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.FindMatchAsync(
            user.Id,
            It.IsAny<string>(),
            It.IsAny<List<string>>()
        )).ReturnsAsync((PeerInterviewMatch?)null);

        var request = new FindMatchRequest
        {
            PreferredDifficulty = "Easy"
        };

        // Act
        var result = await _controller.FindMatch(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task FindMatch_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.FindMatchAsync(
            user.Id,
            It.IsAny<string>(),
            It.IsAny<List<string>>()
        )).ThrowsAsync(new Exception("Database error"));

        var request = new FindMatchRequest();

        // Act
        var result = await _controller.FindMatch(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region UpdateMatchPreferences Tests

    [Fact]
    public async Task UpdateMatchPreferences_WithValidData_UpdatesPreferences()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var match = new PeerInterviewMatch
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreferredDifficulty = "Medium",
            PreferredCategories = System.Text.Json.JsonSerializer.Serialize(new[] { "Arrays", "Trees" }),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.UpdateMatchPreferencesAsync(
            user.Id,
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<bool?>()
        )).ReturnsAsync(match);

        var request = new UpdateMatchPreferencesRequest
        {
            PreferredDifficulty = "Medium",
            PreferredCategories = new List<string> { "Arrays", "Trees" },
            IsAvailable = true
        };

        // Act
        var result = await _controller.UpdateMatchPreferences(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMatch = Assert.IsType<PeerInterviewMatch>(okResult.Value);
        Assert.Equal(match.Id, returnedMatch.Id);
    }

    [Fact]
    public async Task UpdateMatchPreferences_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.UpdateMatchPreferencesAsync(
            user.Id,
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<bool?>()
        )).ThrowsAsync(new Exception("Database error"));

        var request = new UpdateMatchPreferencesRequest();

        // Act
        var result = await _controller.UpdateMatchPreferences(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetMatchPreferences Tests

    [Fact]
    public async Task GetMatchPreferences_WithExistingPreferences_ReturnsPreferences()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        var match = new PeerInterviewMatch
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreferredDifficulty = "Easy",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _peerInterviewServiceMock.Setup(x => x.GetMatchPreferencesAsync(user.Id))
            .ReturnsAsync(match);

        // Act
        var result = await _controller.GetMatchPreferences();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMatch = Assert.IsType<PeerInterviewMatch>(okResult.Value);
        Assert.Equal(match.Id, returnedMatch.Id);
    }

    [Fact]
    public async Task GetMatchPreferences_WithNoPreferences_ReturnsNotFound()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.GetMatchPreferencesAsync(user.Id))
            .ReturnsAsync((PeerInterviewMatch?)null);

        // Act
        var result = await _controller.GetMatchPreferences();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMatchPreferences_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var user = _context.Users.First();
        SetupControllerWithUser(user.Id);

        _peerInterviewServiceMock.Setup(x => x.GetMatchPreferencesAsync(user.Id))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMatchPreferences();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}

