using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Defines an achievement type and how many coins it awards
/// </summary>
public class AchievementDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Unique identifier for the activity type
    /// e.g., 'InterviewCompleted', 'QuestionPublished'
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ActivityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name shown to users
    /// e.g., "You completed a mock interview"
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the achievement
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Number of coins awarded for this achievement
    /// </summary>
    [Required]
    public int CoinsAwarded { get; set; }
    
    /// <summary>
    /// Icon or emoji representing this achievement
    /// e.g., "🪙", "🌟", "🤝"
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    /// <summary>
    /// Whether this achievement is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Maximum number of times this achievement can be earned
    /// Null = unlimited, 1 = one-time only
    /// </summary>
    public int? MaxOccurrences { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
