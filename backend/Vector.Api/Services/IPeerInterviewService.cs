using Vector.Api.DTOs.PeerInterview;

namespace Vector.Api.Services;

/// <summary>
/// Service interface for peer interview functionality
/// </summary>
public interface IPeerInterviewService
{
    // Scheduling
    Task<ScheduledInterviewSessionDto> ScheduleInterviewSessionAsync(Guid userId, ScheduleInterviewDto dto);
    Task<IEnumerable<ScheduledInterviewSessionDto>> GetUpcomingSessionsAsync(Guid userId);
    Task<IEnumerable<ScheduledInterviewSessionDto>> GetPastSessionsAsync(Guid userId);
    Task<ScheduledInterviewSessionDto?> GetScheduledSessionByIdAsync(Guid sessionId, Guid userId);
    Task<bool> CancelScheduledSessionAsync(Guid sessionId, Guid userId);
    
    // Matching
    Task<StartMatchingResponseDto> StartMatchingAsync(Guid scheduledSessionId, Guid userId);
    Task<MatchingRequestDto?> GetMatchingStatusAsync(Guid scheduledSessionId, Guid userId);
    Task<ConfirmMatchResponseDto> ConfirmMatchAsync(Guid matchingRequestId, Guid userId);
    Task<bool> ExpireMatchIfNotConfirmedAsync(Guid matchingRequestId, Guid userId);
    
    // Live Sessions
    Task<LiveInterviewSessionDto?> GetLiveSessionByIdAsync(Guid sessionId, Guid userId);
    Task<SwitchRolesResponseDto> SwitchRolesAsync(Guid sessionId, Guid userId);
    Task<ChangeQuestionResponseDto> ChangeQuestionAsync(Guid sessionId, Guid userId, Guid? newQuestionId = null);
    Task<LiveInterviewSessionDto> EndInterviewAsync(Guid sessionId, Guid userId);
    
    // Feedback
    Task<InterviewFeedbackDto> SubmitFeedbackAsync(Guid userId, SubmitFeedbackDto dto);
    Task<IEnumerable<InterviewFeedbackDto>> GetFeedbackForSessionAsync(Guid sessionId, Guid userId);
    Task<InterviewFeedbackDto?> GetFeedbackAsync(Guid feedbackId, Guid userId);
    Task<FeedbackStatusDto> GetFeedbackStatusAsync(Guid sessionId, Guid userId);
}

