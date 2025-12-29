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
        // Use consistent group name format: "session-{sessionId}"
        // This matches the format used in the controller for sending notifications
        var groupName = $"session-{sessionId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveSession(string sessionId)
    {
        // Use consistent group name format: "session-{sessionId}"
        var groupName = $"session-{sessionId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendCodeUpdate(string sessionId, string code, string language)
    {
        var groupName = $"session-{sessionId}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("CodeUpdated", code, language);
    }

    public async Task SendCodeChange(string sessionId, string code)
    {
        var groupName = $"session-{sessionId}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("CodeChanged", new { 
            userId = Context.UserIdentifier, 
            code = code, 
            timestamp = DateTime.UtcNow.ToString("O") 
        });
    }

    public async Task SendCursorPosition(string sessionId, int line, int column)
    {
        var groupName = $"session-{sessionId}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("CursorMoved", new { 
            userId = Context.UserIdentifier, 
            line = line, 
            column = column, 
            color = "#7c3aed" 
        });
    }

    public async Task SendSelection(string sessionId, object? selection, string color)
    {
        var groupName = $"session-{sessionId}";
        if (selection != null)
        {
            await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("SelectionChanged", new { 
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
            await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("SelectionChanged", null);
        }
    }

    public async Task SendTestResults(string sessionId, object testResults)
    {
        var groupName = $"session-{sessionId}";
        await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("TestResultsUpdated", testResults);
    }
}

