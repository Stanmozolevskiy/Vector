using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Hubs;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class PeerInterviewControllerTests
{
    private readonly Mock<IPeerInterviewService> _peerInterviewServiceMock;
    private readonly Mock<ILogger<PeerInterviewController>> _loggerMock;
    private readonly Mock<IHubContext<CollaborationHub>> _hubContextMock;
    private readonly PeerInterviewController _controller;

    public PeerInterviewControllerTests()
    {
        _peerInterviewServiceMock = new Mock<IPeerInterviewService>();
        _loggerMock = new Mock<ILogger<PeerInterviewController>>();
        _hubContextMock = new Mock<IHubContext<CollaborationHub>>();
        _controller = new PeerInterviewController(
            _peerInterviewServiceMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object);
    }

    private void SetupControllerWithUser(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
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

    // CREATE Tests
    [Fact]
    public async Task ScheduleInterview_WithValidData_ReturnsCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);
        
        var dto = new ScheduleInterviewDto
        {
            InterviewType = "Peer",
            PracticeType = "Mock",
            InterviewLevel = "Beginner",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1)
        };

        var session = new ScheduledInterviewSessionDto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewType = dto.InterviewType,
            PracticeType = dto.PracticeType,
            InterviewLevel = dto.InterviewLevel,
            ScheduledStartAt = dto.ScheduledStartAt,
            Status = "Scheduled"
        };

        _peerInterviewServiceMock.Setup(x => x.ScheduleInterviewSessionAsync(userId, dto))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.ScheduleInterview(dto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, statusCodeResult.StatusCode);
        Assert.Equal(session, statusCodeResult.Value);
        _peerInterviewServiceMock.Verify(x => x.ScheduleInterviewSessionAsync(userId, dto), Times.Once);
    }

    [Fact]
    public async Task ScheduleInterview_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);
        
        var dto = new ScheduleInterviewDto(); // Invalid - missing required fields
        
        _controller.ModelState.AddModelError("ScheduledTime", "ScheduledTime is required");

        // Act
        var result = await _controller.ScheduleInterview(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // READ Tests
    [Fact]
    public async Task GetUpcomingSessions_WithValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);
        
        var sessions = new List<ScheduledInterviewSessionDto>
        {
            new ScheduledInterviewSessionDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "Scheduled",
                ScheduledStartAt = DateTime.UtcNow.AddHours(1)
            }
        };

        _peerInterviewServiceMock.Setup(x => x.GetUpcomingSessionsAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetUpcomingSessions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<ScheduledInterviewSessionDto>>(okResult.Value);
        Assert.Single(returnedSessions);
    }

    [Fact]
    public async Task GetPastSessions_WithValidUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);
        
        var sessions = new List<ScheduledInterviewSessionDto>
        {
            new ScheduledInterviewSessionDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "Completed",
                ScheduledStartAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _peerInterviewServiceMock.Setup(x => x.GetPastSessionsAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetPastSessions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<ScheduledInterviewSessionDto>>(okResult.Value);
        Assert.Single(returnedSessions);
    }

    [Fact]
    public async Task GetScheduledSession_WithValidId_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SetupControllerWithUser(userId);
        
        var session = new ScheduledInterviewSessionDto
        {
            Id = sessionId,
            UserId = userId,
            Status = "Scheduled",
            ScheduledStartAt = DateTime.UtcNow.AddHours(1)
        };

        _peerInterviewServiceMock.Setup(x => x.GetScheduledSessionByIdAsync(sessionId, userId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetScheduledSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(session, okResult.Value);
    }

    [Fact]
    public async Task GetScheduledSession_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        _peerInterviewServiceMock.Setup(x => x.GetScheduledSessionByIdAsync(sessionId, userId))
            .ReturnsAsync((ScheduledInterviewSessionDto?)null);

        // Act
        var result = await _controller.GetScheduledSession(sessionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // UPDATE Tests (Cancel is effectively an update)
    [Fact]
    public async Task CancelScheduledSession_WithValidId_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        _peerInterviewServiceMock.Setup(x => x.CancelScheduledSessionAsync(sessionId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelScheduledSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _peerInterviewServiceMock.Verify(x => x.CancelScheduledSessionAsync(sessionId, userId), Times.Once);
    }

    [Fact]
    public async Task CancelScheduledSession_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        _peerInterviewServiceMock.Setup(x => x.CancelScheduledSessionAsync(sessionId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelScheduledSession(sessionId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // DELETE Tests (Cancel is effectively a delete for scheduled sessions)
    [Fact]
    public async Task CancelScheduledSession_DeletesSession_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        _peerInterviewServiceMock.Setup(x => x.CancelScheduledSessionAsync(sessionId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelScheduledSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // The response is an anonymous type with a message property
        var responseType = okResult.Value.GetType();
        Assert.True(responseType.Name.Contains("AnonymousType") || responseType.GetProperty("message") != null);
    }
}
