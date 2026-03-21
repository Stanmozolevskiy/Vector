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
    private readonly IInterviewMatchingService _matchingService;
    private readonly ILogger<CollaborationHub> _logger;

    public CollaborationHub(
        IMatchingPresenceService presenceService, 
        IInterviewMatchingService matchingService,
        ILogger<CollaborationHub> logger)
    {
        _presenceService = presenceService;
        _matchingService = matchingService;
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
            // Expire any matched requests (user is in confirmation window) immediately
            try
            {
                await _matchingService.ExpireMatchOnUserDisconnectAsync(userId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring match on disconnect for user {UserId}", userId.Value);
            }

            _presenceService.ClearUserPresence(userId.Value);
            _logger.LogDebug("User {UserId} disconnected, cleared all presence and expired matches", userId.Value);
        }
        await base.OnDisconnectedAsync(exception);
    }
    public async Task JoinSession(string sessionId)
    {
        // Ensure sessionId is a string for consistent group naming
        var sessionIdString = sessionId?.ToString() ?? string.Empty;
        _logger.LogInformation("User {ConnectionId} joining session group {SessionId}", Context.ConnectionId, sessionIdString);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionIdString);
        _logger.LogInformation("User {ConnectionId} successfully joined session group {SessionId}", Context.ConnectionId, sessionIdString);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Send a chat message to other users in the session
    /// </summary>
    public async Task SendChatMessage(string sessionId, string message)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Invalid userId in SendChatMessage");
            return;
        }
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("Invalid sessionId in SendChatMessage. UserId: {UserId}", userId.Value);
            return;
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var sessionIdString = sessionId.ToString();
        await Clients.Group(sessionIdString).SendAsync("ChatMessage", new
        {
            sessionId = sessionIdString,
            userId = userId.Value.ToString(),
            message = message.Trim(),
            timestamp = DateTime.UtcNow.ToString("O")
        });
    }

    /// <summary>
    /// Notify other users in the session that roles have been switched
    /// </summary>
    public async Task SendRoleSwitched(string sessionId)
    {
        var sessionIdString = sessionId?.ToString() ?? string.Empty;
        _logger.LogInformation("Role switched in session {SessionId}, notifying other users", sessionIdString);
        
        await Clients.GroupExcept(sessionIdString, Context.ConnectionId).SendAsync("RoleSwitched");
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

    public async Task SendTimerSync(string sessionId, int elapsedTime)
    {
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("TimerSynced", elapsedTime);
    }

    /// <summary>
    /// Broadcast whiteboard update to all other users in the session
    /// </summary>
    public async Task BroadcastWhiteboardUpdate(string sessionId, WhiteboardUpdateData whiteboardData)
    {
        _logger.LogInformation("Broadcasting whiteboard update for session {SessionId}, ConnectionId: {ConnectionId}", sessionId, Context.ConnectionId);
        try
        {
            if (whiteboardData == null)
            {
                _logger.LogWarning("Whiteboard data is null for session {SessionId}", sessionId);
                return;
            }
            
            // Convert sessionId to string to ensure consistent group naming
            var sessionIdString = sessionId?.ToString() ?? string.Empty;
            
            var elementsList = whiteboardData.Elements ?? new List<object>();
            var appStateDict = whiteboardData.AppState ?? new Dictionary<string, object>();
            
            _logger.LogInformation("Sending whiteboard update to group {SessionId}, excluding connection {ConnectionId}. Elements count: {ElementsCount}", 
                sessionIdString, Context.ConnectionId, elementsList.Count);
            
            await Clients.GroupExcept(sessionIdString, Context.ConnectionId).SendAsync("WhiteboardUpdate", new
            {
                sessionId = sessionIdString,
                elements = elementsList,
                appState = appStateDict,
                role = whiteboardData.Role
            });
            
            _logger.LogInformation("Whiteboard update broadcast successfully for session {SessionId} to group", sessionIdString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting whiteboard update for session {SessionId}", sessionId);
            throw;
        }
    }
    
    /// <summary>
    /// Data class for whiteboard updates
    /// </summary>
    public class WhiteboardUpdateData
    {
        public List<object>? Elements { get; set; }
        public Dictionary<string, object>? AppState { get; set; }
        public string? Role { get; set; }
    }

    // ==================== WebRTC Signaling Methods ====================

    /// <summary>
    /// Send WebRTC offer to peer in session
    /// </summary>
    public async Task SendWebRTCOffer(string sessionId, string offer)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Invalid userId in SendWebRTCOffer");
            return;
        }

        _logger.LogDebug("User {UserId} sending WebRTC offer for session {SessionId}", userId.Value, sessionId);
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("WebRTCOffer", new
        {
            userId = userId.Value.ToString(),
            offer = offer
        });
    }

    /// <summary>
    /// Send WebRTC answer to peer in session
    /// </summary>
    public async Task SendWebRTCAnswer(string sessionId, string answer)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Invalid userId in SendWebRTCAnswer");
            return;
        }

        _logger.LogDebug("User {UserId} sending WebRTC answer for session {SessionId}", userId.Value, sessionId);
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("WebRTCAnswer", new
        {
            userId = userId.Value.ToString(),
            answer = answer
        });
    }

    /// <summary>
    /// Send WebRTC ICE candidate to peer in session
    /// </summary>
    public async Task SendWebRTCIceCandidate(string sessionId, string candidate, int? sdpMLineIndex, string? sdpMid)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Invalid userId in SendWebRTCIceCandidate");
            return;
        }

        _logger.LogDebug("User {UserId} sending WebRTC ICE candidate for session {SessionId}", userId.Value, sessionId);
        await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("WebRTCIceCandidate", new
        {
            userId = userId.Value.ToString(),
            candidate = candidate,
            sdpMLineIndex = sdpMLineIndex,
            sdpMid = sdpMid
        });
    }
}

