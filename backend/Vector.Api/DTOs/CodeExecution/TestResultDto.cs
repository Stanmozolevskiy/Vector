namespace Vector.Api.DTOs.CodeExecution;

public class TestResultDto
{
    public Guid TestCaseId { get; set; }
    public int TestCaseNumber { get; set; }
    public bool Passed { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public decimal Runtime { get; set; }
    public long Memory { get; set; }
    public string Status { get; set; } = string.Empty;
}

