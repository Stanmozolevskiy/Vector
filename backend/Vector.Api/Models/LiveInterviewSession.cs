using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents an active live interview session with two participants
/// </summary>
public class LiveInterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to scheduled session (optional, may be null if created directly)
    /// </summary>
    public Guid? ScheduledSessionId { get; set; }
    
    public ScheduledInterviewSession? ScheduledSession { get; set; }
    
    /// <summary>
    /// First question assigned to the session
    /// </summary>
    public Guid? FirstQuestionId { get; set; }
    
    public InterviewQuestion? FirstQuestion { get; set; }
    
    /// <summary>
    /// Second question assigned to the session (optional, for when they switch roles)
    /// </summary>
    public Guid? SecondQuestionId { get; set; }
    
    public InterviewQuestion? SecondQuestion { get; set; }
    
    /// <summary>
    /// Currently active question ID (either FirstQuestionId or SecondQuestionId)
    /// </summary>
    public Guid? ActiveQuestionId { get; set; }
    
    /// <summary>
    /// Status: InProgress, Completed, Cancelled
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "InProgress";
    
    /// <summary>
    /// When the session actually started
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When the session ended
    /// </summary>
    public DateTime? EndedAt { get; set; }
    
    /// <summary>
    /// Excalidraw room ID for the interviewer's whiteboard (for system design interviews)
    /// Format: roomId,key (e.g., "35a7b3f8f24f22d21f18,gaiLKrrJVtanONO5UiU2UA")
    /// </summary>
    [MaxLength(200)]
    public string? InterviewerRoomId { get; set; }
    
    /// <summary>
    /// Excalidraw room ID for the interviewee's whiteboard (for system design interviews)
    /// Format: roomId,key (e.g., "35a7b3f8f24f22d21f18,gaiLKrrJVtanONO5UiU2UA")
    /// </summary>
    [MaxLength(200)]
    public string? IntervieweeRoomId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<LiveInterviewParticipant> Participants { get; set; } = new List<LiveInterviewParticipant>();
    public ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
}

