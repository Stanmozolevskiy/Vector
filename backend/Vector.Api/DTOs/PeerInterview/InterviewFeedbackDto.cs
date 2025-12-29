using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// DTO for submitting interview feedback
/// </summary>
public class SubmitFeedbackDto
{
    [Required]
    public Guid LiveSessionId { get; set; }
    
    [Required]
    public Guid RevieweeId { get; set; }
    
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
}

/// <summary>
/// DTO for feedback response
/// </summary>
public class InterviewFeedbackDto
{
    public Guid Id { get; set; }
    public Guid LiveSessionId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid RevieweeId { get; set; }
    public int? ProblemSolvingRating { get; set; }
    public string? ProblemSolvingDescription { get; set; }
    public int? CodingSkillsRating { get; set; }
    public string? CodingSkillsDescription { get; set; }
    public int? CommunicationRating { get; set; }
    public string? CommunicationDescription { get; set; }
    public string? ThingsDidWell { get; set; }
    public string? AreasForImprovement { get; set; }
    public int? InterviewerPerformanceRating { get; set; }
    public string? InterviewerPerformanceDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public UserDto? Reviewer { get; set; }
    public UserDto? Reviewee { get; set; }
}

