using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IPeerInterviewService
{
    Task<PeerInterviewMatch?> FindMatchAsync(Guid userId, string? preferredDifficulty = null, List<string>? preferredCategories = null);
    Task<PeerInterviewSession> CreateSessionAsync(Guid interviewerId, Guid? intervieweeId = null, Guid? questionId = null, DateTime? scheduledTime = null, int duration = 45, string? interviewType = null, string? practiceType = null, string? interviewLevel = null);
    Task<InterviewMatchingRequest> CreateMatchingRequestAsync(Guid sessionId, Guid userId);
    Task<InterviewMatchingRequest?> FindMatchingPeerAsync(Guid userId, Guid sessionId);
    Task<InterviewMatchingRequest?> ConfirmMatchAsync(Guid matchingRequestId, Guid userId);
    Task<PeerInterviewSession?> CompleteMatchAsync(Guid matchingRequestId);
    Task<PeerInterviewSession?> GetSessionForMatchedRequestAsync(Guid matchingRequestId, Guid userId);
    Task<PeerInterviewSession?> GetSessionByIdAsync(Guid sessionId);
    Task<List<PeerInterviewSession>> GetUserSessionsAsync(Guid userId, string? status = null);
    Task<PeerInterviewSession> UpdateSessionStatusAsync(Guid sessionId, string status);
    Task<bool> CancelSessionAsync(Guid sessionId, Guid userId);
    Task<PeerInterviewMatch> UpdateMatchPreferencesAsync(Guid userId, string? preferredDifficulty = null, List<string>? preferredCategories = null, string? availability = null, bool? isAvailable = null);
    Task<PeerInterviewMatch?> GetMatchPreferencesAsync(Guid userId);
    Task<PeerInterviewSession> ChangeQuestionAsync(Guid sessionId, Guid userId);
    Task<PeerInterviewSession> SwitchRolesAsync(Guid sessionId, Guid userId);
}

