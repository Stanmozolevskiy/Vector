namespace Vector.Api.DTOs.Question;

public class BookmarkDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddBookmarkDto
{
    public string? Notes { get; set; }
}
