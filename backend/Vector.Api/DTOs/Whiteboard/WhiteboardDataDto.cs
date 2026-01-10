namespace Vector.Api.DTOs.Whiteboard;

public class WhiteboardDataDto
{
    public string Id { get; set; } = string.Empty;
    public string? QuestionId { get; set; }
    public string Elements { get; set; } = "[]";
    public string AppState { get; set; } = "{}";
    public string Files { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveWhiteboardDataDto
{
    public string? QuestionId { get; set; }
    public string Elements { get; set; } = "[]";
    public string AppState { get; set; } = "{}";
    public string Files { get; set; } = "{}";
}
