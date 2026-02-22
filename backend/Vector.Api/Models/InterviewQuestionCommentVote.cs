using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vector.Api.Models;

/// <summary>
/// Vote on an interview question comment (one per user per comment)
/// </summary>
public class InterviewQuestionCommentVote
{
    [Required]
    public Guid CommentId { get; set; }

    [JsonIgnore]
    public InterviewQuestionComment Comment { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>
    /// Vote type: 1 for upvote, -1 for downvote
    /// </summary>
    [Required]
    public int VoteType { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

