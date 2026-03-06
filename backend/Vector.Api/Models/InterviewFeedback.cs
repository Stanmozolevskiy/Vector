using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents feedback provided by one user about another after an interview
/// Each participant can leave separate feedback for the other participant
/// </summary>
public class InterviewFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Live session this feedback is for
    /// </summary>
    [Required]
    public Guid LiveSessionId { get; set; }
    
    public LiveInterviewSession LiveSession { get; set; } = null!;
    
    /// <summary>
    /// User who is providing the feedback (reviewer)
    /// </summary>
    [Required]
    public Guid ReviewerId { get; set; }
    
    public User Reviewer { get; set; } = null!;
    
    /// <summary>
    /// User who is being reviewed (reviewee)
    /// </summary>
    [Required]
    public Guid RevieweeId { get; set; }
    
    public User Reviewee { get; set; } = null!;
    
    /// <summary>
    /// Problem solving rating (1-5)
    /// </summary>
    [Range(1, 5)]
    public int? ProblemSolvingRating { get; set; }
    
    /// <summary>
    /// Problem solving description
    /// </summary>
    public string? ProblemSolvingDescription { get; set; }
    
    /// <summary>
    /// Coding skills rating (1-5)
    /// </summary>
    [Range(1, 5)]
    public int? CodingSkillsRating { get; set; }
    
    /// <summary>
    /// Coding skills description
    /// </summary>
    public string? CodingSkillsDescription { get; set; }
    
    /// <summary>
    /// Communication rating (1-5)
    /// </summary>
    [Range(1, 5)]
    public int? CommunicationRating { get; set; }
    
    /// <summary>
    /// Communication description
    /// </summary>
    public string? CommunicationDescription { get; set; }
    
    /// <summary>
    /// Things the reviewee did well
    /// </summary>
    public string? ThingsDidWell { get; set; }
    
    /// <summary>
    /// Areas where the reviewee could improve
    /// </summary>
    public string? AreasForImprovement { get; set; }
    
    /// <summary>
    /// Overall interviewer performance rating (1-5)
    /// </summary>
    [Range(1, 5)]
    public int? InterviewerPerformanceRating { get; set; }
    
    /// <summary>
    /// Interviewer performance description
    /// </summary>
    public string? InterviewerPerformanceDescription { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

