using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Upvote on an interview question comment (one per user per comment)
/// </summary>
public class InterviewQuestionCommentVote
{
    [Required]
    public Guid CommentId { get; set; }

    public InterviewQuestionComment Comment { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

