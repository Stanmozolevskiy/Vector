using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a single coin transaction (earning or spending)
/// </summary>
public class CoinTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Amount of coins (positive for earning, negative for spending)
    /// </summary>
    [Required]
    public int Amount { get; set; }
    
    /// <summary>
    /// Type of activity that triggered this transaction
    /// e.g., 'InterviewCompleted', 'QuestionPublished'
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ActivityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of the transaction
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Reference to related entity (question, interview, etc.)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
    
    /// <summary>
    /// Type of related entity (e.g., 'Question', 'Interview')
    /// </summary>
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
