using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a solution for an interview question
/// </summary>
public class QuestionSolution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid QuestionId { get; set; }
    
    public InterviewQuestion Question { get; set; } = null!;
    
    /// <summary>
    /// Programming language: Python, JavaScript, Java, C++, etc.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// Solution code
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Solution explanation
    /// </summary>
    public string? Explanation { get; set; }
    
    /// <summary>
    /// Time complexity (e.g., "O(n)")
    /// </summary>
    [MaxLength(50)]
    public string? TimeComplexity { get; set; }
    
    /// <summary>
    /// Space complexity (e.g., "O(1)")
    /// </summary>
    [MaxLength(50)]
    public string? SpaceComplexity { get; set; }
    
    /// <summary>
    /// Whether this is an official solution (vs user-submitted)
    /// </summary>
    public bool IsOfficial { get; set; } = false;
    
    /// <summary>
    /// User who created this solution (null if official)
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    public User? Creator { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

