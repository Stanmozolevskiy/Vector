namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// DTO for interview matching request
/// </summary>
public class MatchingRequestDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ScheduledSessionId { get; set; }
    public string InterviewType { get; set; } = string.Empty;
    public string PracticeType { get; set; } = string.Empty;
    public string InterviewLevel { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Matched, Confirmed, Expired, Cancelled
    public Guid? MatchedUserId { get; set; }
    public Guid? LiveSessionId { get; set; }
    public bool UserConfirmed { get; set; }
    public bool MatchedUserConfirmed { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public UserDto? User { get; set; }
    public UserDto? MatchedUser { get; set; }
    public ScheduledInterviewSessionDto? ScheduledSession { get; set; }
}

public class StartMatchingResponseDto
{
    public MatchingRequestDto MatchingRequest { get; set; } = null!;
    public bool Matched { get; set; }
    public bool SessionComplete { get; set; }
    public LiveInterviewSessionDto? Session { get; set; }
}

public class ConfirmMatchResponseDto
{
    public MatchingRequestDto MatchingRequest { get; set; } = null!;
    public bool Completed { get; set; }
    public LiveInterviewSessionDto? Session { get; set; }
}

