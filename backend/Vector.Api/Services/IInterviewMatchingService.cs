using Vector.Api.DTOs.PeerInterview;

namespace Vector.Api.Services;

/// <summary>
/// Service interface for interview matching functionality
/// Handles matching users for peer interviews
/// </summary>
public interface IInterviewMatchingService
{
    /// <summary>
    /// Start matching process for a scheduled session
    /// </summary>
    Task<StartMatchingResponseDto> StartMatchingAsync(Guid scheduledSessionId, Guid userId);

    /// <summary>
    /// Get matching status for a scheduled session
    /// </summary>
    Task<MatchingRequestDto?> GetMatchingStatusAsync(Guid scheduledSessionId, Guid userId);

    /// <summary>
    /// Confirm a match (user confirms readiness)
    /// </summary>
    Task<ConfirmMatchResponseDto> ConfirmMatchAsync(Guid matchingRequestId, Guid userId);

    /// <summary>
    /// Expire a match if not both confirmed within 15 seconds
    /// </summary>
    Task<bool> ExpireMatchIfNotConfirmedAsync(Guid matchingRequestId, Guid userId);

    /// <summary>
    /// Expire all active matching requests for a user and session (used on page refresh/close)
    /// Does not create new requests
    /// </summary>
    Task ExpireAllRequestsForSessionAsync(Guid scheduledSessionId, Guid userId);
}
