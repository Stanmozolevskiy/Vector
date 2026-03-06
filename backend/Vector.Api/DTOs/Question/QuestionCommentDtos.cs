namespace Vector.Api.DTOs.Question;

public class CreateQuestionCommentDto
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

public class QuestionCommentDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserProfilePictureUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? ParentCommentId { get; set; }
    public int UpvoteCount { get; set; }
    public bool HasUpvoted { get; set; }
    public List<QuestionCommentDto> Replies { get; set; } = new();
}

