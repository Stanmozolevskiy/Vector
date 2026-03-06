using System.Collections.Concurrent;

namespace Vector.Api.Services;

/// <summary>
/// In-memory service for tracking user presence in the matching modal
/// Uses ConcurrentDictionary for thread-safe operations
/// </summary>
public class MatchingPresenceService : IMatchingPresenceService
{
    // Key: userId, Value: HashSet of sessionIds where user is active
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _activeUsers = new();

    public void SetUserActive(Guid userId, Guid sessionId)
    {
        _activeUsers.AddOrUpdate(
            userId,
            new HashSet<Guid> { sessionId },
            (key, existingSet) =>
            {
                existingSet.Add(sessionId);
                return existingSet;
            });
    }

    public void SetUserInactive(Guid userId, Guid sessionId)
    {
        if (_activeUsers.TryGetValue(userId, out var sessionSet))
        {
            sessionSet.Remove(sessionId);
            if (sessionSet.Count == 0)
            {
                _activeUsers.TryRemove(userId, out _);
            }
        }
    }

    public bool IsUserActive(Guid userId, Guid sessionId)
    {
        if (_activeUsers.TryGetValue(userId, out var sessionSet))
        {
            return sessionSet.Contains(sessionId);
        }
        return false;
    }

    public void ClearUserPresence(Guid userId)
    {
        _activeUsers.TryRemove(userId, out _);
    }
}
