using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents learning analytics and progress tracking for a user
/// </summary>
public class LearningAnalytics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Total number of questions solved (with accepted solutions)
    /// </summary>
    public int QuestionsSolved { get; set; } = 0;
    
    /// <summary>
    /// Questions solved by category (JSON object): {"Arrays": 5, "Strings": 3, ...}
    /// </summary>
    public string? QuestionsByCategory { get; set; }
    
    /// <summary>
    /// Questions solved by difficulty (JSON object): {"Easy": 10, "Medium": 5, "Hard": 2}
    /// </summary>
    public string? QuestionsByDifficulty { get; set; }
    
    /// <summary>
    /// Average execution time across all accepted solutions (in seconds)
    /// </summary>
    public decimal AverageExecutionTime { get; set; } = 0;
    
    /// <summary>
    /// Average memory used across all accepted solutions (in bytes)
    /// </summary>
    public long AverageMemoryUsed { get; set; } = 0;
    
    /// <summary>
    /// Success rate (percentage of accepted solutions)
    /// </summary>
    public decimal SuccessRate { get; set; } = 0;
    
    /// <summary>
    /// Current streak (consecutive days with at least one accepted solution)
    /// </summary>
    public int CurrentStreak { get; set; } = 0;
    
    /// <summary>
    /// Longest streak achieved
    /// </summary>
    public int LongestStreak { get; set; } = 0;
    
    /// <summary>
    /// Date of last activity (last accepted solution)
    /// </summary>
    public DateTime? LastActivityDate { get; set; }
    
    /// <summary>
    /// Total number of submissions (including failed attempts)
    /// </summary>
    public int TotalSubmissions { get; set; } = 0;
    
    /// <summary>
    /// Solutions by language (JSON object): {"python": 10, "javascript": 5, ...}
    /// </summary>
    public string? SolutionsByLanguage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

