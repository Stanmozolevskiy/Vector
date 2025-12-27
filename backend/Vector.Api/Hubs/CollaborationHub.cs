using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vector.Api.Data;
using Vector.Api.Services;

namespace Vector.Api.Hubs;

[Authorize]
public class CollaborationHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly IPeerInterviewService _peerInterviewService;
    private readonly ILogger<CollaborationHub> _logger;

    public CollaborationHub(
        ApplicationDbContext context,
        IPeerInterviewService peerInterviewService,
        ILogger<CollaborationHub> logger)
    {
        _context = context;
        _peerInterviewService = peerInterviewService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected to SignalR hub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected from SignalR hub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSession(string sessionId)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await Clients.Caller.SendAsync("Error", "Invalid session ID");
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session not found");
            return;
        }

        if (session.InterviewerId != userId && session.IntervieweeId != userId)
        {
            await Clients.Caller.SendAsync("Error", "Access denied");
            return;
        }

        var groupName = $"session-{sessionGuid}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync("UserJoined", new
        {
            userId = userId.ToString(),
            connectionId = Context.ConnectionId
        });
    }

    public async Task SendCodeChange(string sessionId, string code)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await Clients.Caller.SendAsync("Error", "Invalid session ID");
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null || (session.InterviewerId != userId && session.IntervieweeId != userId))
        {
            await Clients.Caller.SendAsync("Error", "Access denied");
            return;
        }

        var groupName = $"session-{sessionGuid}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("CodeChanged", new
        {
            userId = userId.ToString(),
            code,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendCursorPosition(string sessionId, int line, int column, string? color = null)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null || (session.InterviewerId != userId && session.IntervieweeId != userId))
        {
            return;
        }

        var groupName = $"session-{sessionGuid}";

        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("CursorMoved", new
        {
            userId = userId.ToString(),
            line,
            column,
            color = GetUserColor(userId)
        });
    }

    public async Task SendSelection(string sessionId, object? selection)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null || (session.InterviewerId != userId && session.IntervieweeId != userId))
        {
            return;
        }

        var groupName = $"session-{sessionGuid}";

        if (selection != null)
        {
            // Determine color based on role (interviewer = purple, interviewee = green)
            string userColor;
            if (session.InterviewerId == userId)
            {
                userColor = "#7c3aed"; // Purple for interviewer
            }
            else if (session.IntervieweeId == userId)
            {
                userColor = "#10b981"; // Green for interviewee
            }
            else
            {
                userColor = GetUserColor(userId);
            }
            
            await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("SelectionChanged", new
            {
                userId = userId.ToString(),
                startLine = ((dynamic)selection).startLine,
                startColumn = ((dynamic)selection).startColumn,
                endLine = ((dynamic)selection).endLine,
                endColumn = ((dynamic)selection).endColumn,
                color = userColor
            });
        }
        else
        {
            await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("SelectionChanged", null);
        }
    }

    public async Task SendTestResults(string sessionId, object testResults)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null || (session.InterviewerId != userId && session.IntervieweeId != userId))
        {
            return;
        }

        var groupName = $"session-{sessionGuid}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("TestResultsUpdated", testResults);
    }

    public async Task SendRoleSwitched(string sessionId)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null || (session.InterviewerId != userId && session.IntervieweeId != userId))
        {
            return;
        }

        var groupName = $"session-{sessionGuid}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("RoleSwitched", new { sessionId = sessionGuid.ToString() });
    }

    public async Task SendQuestionChanged(string sessionId, string questionId)
    {
        var userId = GetUserId();

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            return;
        }

        // Verify user has access to this session
        var session = await _peerInterviewService.GetSessionByIdAsync(sessionGuid);
        if (session == null || (session.InterviewerId != userId && session.IntervieweeId != userId))
        {
            return;
        }

        var groupName = $"session-{sessionGuid}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("QuestionChanged", new { sessionId = sessionGuid.ToString(), questionId });
    }

    private string GetUserColor(Guid userId)
    {
        // Generate a consistent color for each user
        var hash = userId.GetHashCode();
        var colors = new[]
        {
            "#7c3aed", // Purple
            "#10b981", // Green
            "#f59e0b", // Amber
            "#ef4444", // Red
            "#3b82f6", // Blue
            "#ec4899", // Pink
        };
        return colors[Math.Abs(hash) % colors.Length];
    }
}

