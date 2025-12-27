namespace Vector.Api.DTOs.Solution;

public class UserSolutionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionTitle { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ExecutionTime { get; set; }
    public long MemoryUsed { get; set; }
    public int TestCasesPassed { get; set; }
    public int TotalTestCases { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<SolutionSubmissionDto> TestCaseResults { get; set; } = new();
}

