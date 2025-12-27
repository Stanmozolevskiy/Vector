namespace Vector.Api.DTOs.Solution;

public class SolutionSubmissionDto
{
    public Guid Id { get; set; }
    public Guid TestCaseId { get; set; }
    public int TestCaseNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? ExpectedOutput { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal ExecutionTime { get; set; }
    public long MemoryUsed { get; set; }
}

