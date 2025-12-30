using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Vector.Api.Hubs;

/// <summary>
/// SignalR hub for real-time collaboration during peer interviews
/// </summary>
[Authorize]
public class CollaborationHub : Hub
{
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

