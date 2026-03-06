using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a scheduled interview session created by a user
/// </summary>
public class ScheduledInterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User who scheduled the interview (session creator)
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Interview type: data-structures-algorithms, system-design, behavioral, product-management, sql, data-science-ml
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InterviewType { get; set; } = string.Empty;
    
    /// <summary>
    /// Practice type: peers, friend, expert
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PracticeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Interview level: beginner, intermediate, advanced
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InterviewLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// Scheduled start time for the interview
    /// </summary>
    [Required]
    public DateTime ScheduledStartAt { get; set; }
    
    /// <summary>
    /// Status: Scheduled, Cancelled, Completed, InProgress
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Scheduled";
    
    /// <summary>
    /// Reference to live session if one was created from this scheduled session
    /// (navigation property only - foreign key is on LiveInterviewSession)
    /// </summary>
    public LiveInterviewSession? LiveSession { get; set; }
    
    /// <summary>
    /// Question assigned to this user when scheduling (only for data-structures-algorithms interview type)
    /// </summary>
    public Guid? AssignedQuestionId { get; set; }
    
    public InterviewQuestion? AssignedQuestion { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

