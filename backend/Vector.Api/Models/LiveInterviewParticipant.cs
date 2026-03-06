using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a participant in a live interview session
/// Stores per-user metadata including their role
/// </summary>
public class LiveInterviewParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Live session this participant belongs to
    /// </summary>
    [Required]
    public Guid LiveSessionId { get; set; }
    
    public LiveInterviewSession LiveSession { get; set; } = null!;
    
    /// <summary>
    /// User who is participating
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Current role: Interviewer or Interviewee
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Interviewee"; // Interviewer or Interviewee
    
    /// <summary>
    /// Whether this participant is currently in the session
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the participant joined the session
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the participant left the session (if they left early)
    /// </summary>
    public DateTime? LeftAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

