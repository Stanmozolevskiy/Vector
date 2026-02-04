using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// User comment on an interview question (community answers / discussion)
/// </summary>
public class InterviewQuestionComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid QuestionId { get; set; }

    public InterviewQuestion Question { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>
    /// Parent comment for threaded replies (null for top-level comments)
    /// </summary>
    public Guid? ParentCommentId { get; set; }

    public InterviewQuestionComment? ParentComment { get; set; }

    public ICollection<InterviewQuestionComment> Replies { get; set; } = new List<InterviewQuestionComment>();

    public ICollection<InterviewQuestionCommentVote> Votes { get; set; } = new List<InterviewQuestionCommentVote>();

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

