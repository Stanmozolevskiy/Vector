using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a matching request in the queue for pairing users
/// Status transitions: Pending → Matched → Confirmed
/// </summary>
public class InterviewMatchingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User who initiated the matching request
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Scheduled session that triggered this matching request
    /// </summary>
    [Required]
    public Guid ScheduledSessionId { get; set; }
    
    public ScheduledInterviewSession ScheduledSession { get; set; } = null!;
    
    /// <summary>
    /// Interview type (hard match requirement)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InterviewType { get; set; } = string.Empty;
    
    /// <summary>
    /// Practice type (hard match requirement)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PracticeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Interview level (soft match - preferred but not required)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InterviewLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// Scheduled start time for matching
    /// </summary>
    [Required]
    public DateTime ScheduledStartAt { get; set; }
    
    /// <summary>
    /// Status: Pending, Matched, Confirmed, Expired, Cancelled
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// ID of the matched user (when status is Matched or Confirmed)
    /// </summary>
    public Guid? MatchedUserId { get; set; }
    
    public User? MatchedUser { get; set; }
    
    /// <summary>
    /// ID of the live session created when match is confirmed
    /// </summary>
    public Guid? LiveSessionId { get; set; }
    
    public LiveInterviewSession? LiveSession { get; set; }
    
    /// <summary>
    /// Whether the requesting user has confirmed readiness
    /// </summary>
    public bool UserConfirmed { get; set; } = false;
    
    /// <summary>
    /// Whether the matched user has confirmed readiness
    /// </summary>
    public bool MatchedUserConfirmed { get; set; } = false;
    
    /// <summary>
    /// When the matching request expires (10 minutes after creation)
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

