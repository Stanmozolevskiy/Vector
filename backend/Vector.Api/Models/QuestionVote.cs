using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a user's vote (upvote/downvote) on a question
/// </summary>
public class QuestionVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The question that was voted on
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }
    
    public InterviewQuestion Question { get; set; } = null!;
    
    /// <summary>
    /// User who voted
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Vote type: 1 for upvote, -1 for downvote
    /// </summary>
    [Required]
    public int VoteType { get; set; } // 1 = upvote, -1 = downvote
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
