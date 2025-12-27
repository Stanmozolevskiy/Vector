using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

/// <summary>
/// Tracks each user's participation in a session independently.
/// This allows one user to cancel without affecting the other user's view.
/// </summary>
public class UserSessionParticipant
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    [ForeignKey("Session")]
    public Guid SessionId { get; set; }
    public PeerInterviewSession Session { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Interviewer"; // Interviewer or Interviewee

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Left, Cancelled, Completed

    /// <summary>
    /// When the user joined/started the session
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// When the user left the session (if they left early)
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    /// Whether the user is currently connected (for network reconnection)
    /// </summary>
    public bool IsConnected { get; set; } = true;

    /// <summary>
    /// Last time the user was seen active (for detecting network issues)
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}




