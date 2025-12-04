namespace Vector.Api.Models;

public class MockInterview
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string Category { get; set; } = string.Empty; // e.g., "System Design", "Behavioral", "Coding"
    public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

