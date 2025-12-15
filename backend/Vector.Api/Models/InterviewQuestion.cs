using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents an interview question (LeetCode-style problem)
/// </summary>
public class InterviewQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Difficulty level: Easy, Medium, Hard
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Difficulty { get; set; } = "Medium";
    
    /// <summary>
    /// Question type: Coding, Behavioral, Design, Whiteboard, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = "Coding";
    
    /// <summary>
    /// Category: Arrays, Strings, Trees, Graphs, Dynamic Programming, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Company tags (JSON array): ["Google", "Amazon", "Facebook"]
    /// </summary>
    public string? CompanyTags { get; set; }
    
    /// <summary>
    /// Additional tags (JSON array): ["Hash Table", "Two Pointers"]
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// Constraints as text
    /// </summary>
    public string? Constraints { get; set; }
    
    /// <summary>
    /// Examples (JSON array of example objects)
    /// </summary>
    public string? Examples { get; set; }
    
    /// <summary>
    /// Hints (JSON array of strings)
    /// </summary>
    public string? Hints { get; set; }
    
    /// <summary>
    /// Time complexity hint (e.g., "O(n)")
    /// </summary>
    [MaxLength(50)]
    public string? TimeComplexityHint { get; set; }
    
    /// <summary>
    /// Space complexity hint (e.g., "O(1)")
    /// </summary>
    [MaxLength(50)]
    public string? SpaceComplexityHint { get; set; }
    
    /// <summary>
    /// Acceptance rate (percentage)
    /// </summary>
    public double? AcceptanceRate { get; set; }
    
    /// <summary>
    /// Whether the question is active and visible
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// User who created the question (admin or coach)
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    public User? Creator { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<QuestionTestCase> TestCases { get; set; } = new List<QuestionTestCase>();
    public ICollection<QuestionSolution> Solutions { get; set; } = new List<QuestionSolution>();
}

