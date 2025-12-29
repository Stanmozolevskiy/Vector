namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// DTO for scheduled interview session response
/// </summary>
public class ScheduledInterviewSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string InterviewType { get; set; } = string.Empty;
    public string PracticeType { get; set; } = string.Empty;
    public string InterviewLevel { get; set; } = string.Empty;
    public DateTime ScheduledStartAt { get; set; }
    public string Status { get; set; } = "Scheduled";
    public Guid? LiveSessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public UserDto? User { get; set; }
    public LiveInterviewSessionDto? LiveSession { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
}

