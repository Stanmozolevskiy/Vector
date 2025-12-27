using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

public class InterviewMatchingRequest
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    [ForeignKey("ScheduledSession")]
    public Guid ScheduledSessionId { get; set; }
    public PeerInterviewSession ScheduledSession { get; set; } = null!;

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Matched, Confirmed, Cancelled, Expired

    [ForeignKey("MatchedUser")]
    public Guid? MatchedUserId { get; set; }
    public User? MatchedUser { get; set; }

    [ForeignKey("MatchedRequest")]
    public Guid? MatchedRequestId { get; set; }
    public InterviewMatchingRequest? MatchedRequest { get; set; }

    public bool UserConfirmed { get; set; } = false;
    public bool MatchedUserConfirmed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; } // Expires after 5 minutes if not matched
}





