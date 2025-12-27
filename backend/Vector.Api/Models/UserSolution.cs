using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a user's solution submission for an interview question
/// </summary>
public class UserSolution
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
    /// Submitted code
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Overall status: Accepted, Wrong Answer, Time Limit Exceeded, Runtime Error, Compilation Error
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Wrong Answer";
    
    /// <summary>
    /// Execution time in seconds
    /// </summary>
    public decimal ExecutionTime { get; set; }
    
    /// <summary>
    /// Memory used in bytes
    /// </summary>
    public long MemoryUsed { get; set; }
    
    /// <summary>
    /// Number of test cases passed
    /// </summary>
    public int TestCasesPassed { get; set; }
    
    /// <summary>
    /// Total number of test cases
    /// </summary>
    public int TotalTestCases { get; set; }
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property for test case results
    public ICollection<SolutionSubmission> TestCaseResults { get; set; } = new List<SolutionSubmission>();
}

