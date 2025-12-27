using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

public class PeerInterviewSession
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("Interviewer")]
    public Guid InterviewerId { get; set; }
    public User Interviewer { get; set; } = null!;

    [ForeignKey("Interviewee")]
    public Guid? IntervieweeId { get; set; }
    public User? Interviewee { get; set; }

    [ForeignKey("Question")]
    public Guid? QuestionId { get; set; }
    public InterviewQuestion? Question { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Scheduled"; // Scheduled, InProgress, Completed, Cancelled

    public DateTime? ScheduledTime { get; set; }

    public int Duration { get; set; } = 45; // Duration in minutes

    [MaxLength(500)]
    public string? SessionRecordingUrl { get; set; }

    [MaxLength(100)]
    public string? InterviewType { get; set; } // Data Structures & Algorithms, System Design, Behavioral, etc.

    [MaxLength(50)]
    public string? PracticeType { get; set; } // Practice with peers, Practice with a friend, Expert mock interview

    [MaxLength(50)]
    public string? InterviewLevel { get; set; } // Beginner, Intermediate, Advanced

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

