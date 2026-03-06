using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a user's code draft (work in progress) for a question and language
/// </summary>
public class UserCodeDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    [Required]
    public Guid QuestionId { get; set; }
    
    public InterviewQuestion Question { get; set; } = null!;
    
    /// <summary>
    /// Programming language used (python, javascript, java, cpp, csharp, go)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// Draft code (work in progress)
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

