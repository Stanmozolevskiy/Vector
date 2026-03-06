using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// DTO for scheduling a new interview session
/// </summary>
public class ScheduleInterviewDto
{
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
}

