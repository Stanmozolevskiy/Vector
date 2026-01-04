using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Vector.Api.Services;

namespace Vector.Api.Hubs;

/// <summary>
/// SignalR hub for real-time collaboration during peer interviews
/// </summary>
[Authorize]
public class CollaborationHub : Hub
{
    private readonly IMatchingPresenceService _presenceService;
    private readonly ILogger<CollaborationHub> _logger;

    public CollaborationHub(IMatchingPresenceService presenceService, ILogger<CollaborationHub> logger)
    {
        _presenceService = presenceService;
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? Context.User?.FindFirst("sub")?.Value 
                        ?? Context.User?.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }

    /// <summary>
    /// Set user as active in the matching modal for a specific session
    /// </summary>
    public Task SetMatchingModalOpen(string sessionId)
    {
        var userId = GetUserId();
        if (!userId.HasValue || !Guid.TryParse(sessionId, out var sessionGuid))
        {
            _logger.LogWarning("Invalid userId or sessionId in SetMatchingModalOpen. UserId: {UserId}, SessionId: {SessionId}", 
                userId, sessionId);
            return Task.CompletedTask;
        }

        _presenceService.SetUserActive(userId.Value, sessionGuid);
        _logger.LogDebug("User {UserId} set as active for session {SessionId}", userId.Value, sessionGuid);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Set user as inactive (matching modal closed)
    /// </summary>
    public Task SetMatchingModalClosed(string sessionId)
    {
        var userId = GetUserId();
        if (!userId.HasValue || !Guid.TryParse(sessionId, out var sessionGuid))
        {
            _logger.LogWarning("Invalid userId or sessionId in SetMatchingModalClosed. UserId: {UserId}, SessionId: {SessionId}", 
                userId, sessionId);
            return Task.CompletedTask;
        }

        _presenceService.SetUserInactive(userId.Value, sessionGuid);
        _logger.LogDebug("User {UserId} set as inactive for session {SessionId}", userId.Value, sessionGuid);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _presenceService.ClearUserPresence(userId.Value);
            _logger.LogDebug("User {UserId} disconnected, cleared all presence", userId.Value);
        }
        await base.OnDisconnectedAsync(exception);
    }
    public async Task JoinSession(string sessionId)
    {
        // Use sessionId directly as group name (no "session-" prefix)
        // This matches the format used in the controller
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task SendCodeUpdate(string sessionId, string code, string language)
    {
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("CodeUpdated", code, language);
    }

    public async Task SendCodeChange(string sessionId, string code)
    {
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("CodeChanged", new { 
            userId = Context.UserIdentifier, 
            code = code, 
            timestamp = DateTime.UtcNow.ToString("O") 
        });
    }

    public async Task SendCursorPosition(string sessionId, int line, int column)
    {
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("CursorMoved", new { 
            userId = Context.UserIdentifier, 
            line = line, 
            column = column, 
            color = "#7c3aed" 
        });
    }

    public async Task SendSelection(string sessionId, object? selection, string color)
    {
        if (selection != null)
        {
            await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("SelectionChanged", new { 
                userId = Context.UserIdentifier, 
                startLine = ((dynamic)selection).startLine, 
                startColumn = ((dynamic)selection).startColumn, 
                endLine = ((dynamic)selection).endLine, 
                endColumn = ((dynamic)selection).endColumn, 
                color = color 
            });
        }
        else
        {
            await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("SelectionChanged", null);
        }
    }

    public async Task SendTestResults(string sessionId, object testResults)
    {
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("TestResultsUpdated", testResults);
    }
}

