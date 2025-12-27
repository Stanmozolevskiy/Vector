namespace Vector.Api.Models;

/// <summary>
/// Tracks which questions a user has solved (optimized lookup table)
/// </summary>
public class UserSolvedQuestion
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public DateTime SolvedAt { get; set; }
    public string? Language { get; set; } // Language used to solve (optional, for reference)
    
    // Navigation properties
    public User User { get; set; } = null!;
    public InterviewQuestion Question { get; set; } = null!;
}

