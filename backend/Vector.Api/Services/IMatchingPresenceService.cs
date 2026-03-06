namespace Vector.Api.Services;

/// <summary>
/// Service for tracking user presence in the matching modal
/// </summary>
public interface IMatchingPresenceService
{
    /// <summary>
    /// Set user as active in the matching modal for a specific session
    /// </summary>
    void SetUserActive(Guid userId, Guid sessionId);

    /// <summary>
    /// Set user as inactive (modal closed)
    /// </summary>
    void SetUserInactive(Guid userId, Guid sessionId);

    /// <summary>
    /// Check if user is currently active in the matching modal
    /// </summary>
    bool IsUserActive(Guid userId, Guid sessionId);

    /// <summary>
    /// Clear all presence for a user (e.g., on disconnect)
    /// </summary>
    void ClearUserPresence(Guid userId);
}
