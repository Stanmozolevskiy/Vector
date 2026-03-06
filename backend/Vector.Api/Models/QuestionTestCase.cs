using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a test case for an interview question
/// </summary>
public class QuestionTestCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid QuestionId { get; set; }
    
    public InterviewQuestion Question { get; set; } = null!;
    
    /// <summary>
    /// Test case number (for ordering)
    /// </summary>
    public int TestCaseNumber { get; set; }
    
    /// <summary>
    /// Input data (JSON string)
    /// </summary>
    [Required]
    public string Input { get; set; } = string.Empty;
    
    /// <summary>
    /// Expected output (JSON string)
    /// </summary>
    [Required]
    public string ExpectedOutput { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this is a hidden test case (not shown to user before submission)
    /// </summary>
    public bool IsHidden { get; set; } = false;
    
    /// <summary>
    /// Explanation for this test case (optional)
    /// </summary>
    public string? Explanation { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

