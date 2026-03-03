using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

/// <summary>
/// Represents a daily coding challenge for users
/// </summary>
public class DailyChallenge
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The date this challenge is active for
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Reference to the interview question for this challenge
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public InterviewQuestion Question { get; set; } = null!;

    /// <summary>
    /// Difficulty level (Easy, Medium, Hard)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>
    /// Category of the challenge (Arrays, Strings, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Number of users who attempted this challenge
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Number of users who completed this challenge
    /// </summary>
    public int CompletionCount { get; set; } = 0;

    /// <summary>
    /// When this challenge was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this challenge is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
