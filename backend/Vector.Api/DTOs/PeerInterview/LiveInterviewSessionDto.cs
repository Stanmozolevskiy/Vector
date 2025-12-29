namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// DTO for live interview session
/// </summary>
public class LiveInterviewSessionDto
{
    public Guid Id { get; set; }
    public Guid? ScheduledSessionId { get; set; }
    public Guid? FirstQuestionId { get; set; }
    public Guid? SecondQuestionId { get; set; }
    public Guid? ActiveQuestionId { get; set; }
    public string Status { get; set; } = "InProgress";
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public QuestionSummaryDto? FirstQuestion { get; set; }
    public QuestionSummaryDto? SecondQuestion { get; set; }
    public QuestionSummaryDto? ActiveQuestion { get; set; }
    public List<ParticipantDto>? Participants { get; set; }
}

public class QuestionSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
}

public class ParticipantDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Interviewee"; // Interviewer or Interviewee
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public UserDto? User { get; set; }
}

