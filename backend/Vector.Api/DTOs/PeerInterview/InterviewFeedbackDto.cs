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
    /// Whether the session actually happened
    /// </summary>
    public bool? DidSessionHappen { get; set; }
    
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
    
    /// <summary>
    /// Audio/video issues (yes/no/null)
    /// </summary>
    public string? AudioVideoIssues { get; set; }
    
    /// <summary>
    /// Code editor issues (yes/no/null)
    /// </summary>
    public string? CodeEditorIssues { get; set; }
    
    /// <summary>
    /// Additional feedback for Exponent
    /// </summary>
    public string? AdditionalFeedback { get; set; }
    
    /// <summary>
    /// Whether user wants email introduction to partner (yes/no/null)
    /// </summary>
    public string? WantEmailIntroduction { get; set; }
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

/// <summary>
/// DTO for feedback status check
/// </summary>
public class FeedbackStatusDto
{
    /// <summary>
    /// Whether the current user has submitted feedback for their opponent
    /// </summary>
    public bool HasUserSubmittedFeedback { get; set; }
    
    /// <summary>
    /// Whether the opponent has submitted feedback for the current user
    /// </summary>
    public bool HasOpponentSubmittedFeedback { get; set; }
    
    /// <summary>
    /// The opponent's user information
    /// </summary>
    public UserDto? Opponent { get; set; }
    
    /// <summary>
    /// The opponent's ID
    /// </summary>
    public Guid? OpponentId { get; set; }
    
    /// <summary>
    /// The live session ID
    /// </summary>
    public Guid LiveSessionId { get; set; }
    
    /// <summary>
    /// Feedback left by opponent for the current user (if available)
    /// </summary>
    public InterviewFeedbackDto? OpponentFeedback { get; set; }
}
