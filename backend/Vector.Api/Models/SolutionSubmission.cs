using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents the result of a solution submission against a specific test case
/// </summary>
public class SolutionSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserSolutionId { get; set; }
    
    public UserSolution UserSolution { get; set; } = null!;
    
    [Required]
    public Guid TestCaseId { get; set; }
    
    public QuestionTestCase TestCase { get; set; } = null!;
    
    /// <summary>
    /// Test case number for ordering
    /// </summary>
    public int TestCaseNumber { get; set; }
    
    /// <summary>
    /// Status: Passed, Failed
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Failed";
    
    /// <summary>
    /// Output from code execution
    /// </summary>
    public string? Output { get; set; }
    
    /// <summary>
    /// Expected output
    /// </summary>
    public string? ExpectedOutput { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Execution time for this test case in seconds
    /// </summary>
    public decimal ExecutionTime { get; set; }
    
    /// <summary>
    /// Memory used for this test case in bytes
    /// </summary>
    public long MemoryUsed { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

