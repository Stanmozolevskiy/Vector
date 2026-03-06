namespace Vector.Api.DTOs.CodeExecution;

public class TestResultDto
{
    public Guid TestCaseId { get; set; }
    public int TestCaseNumber { get; set; }
    public bool Passed { get; set; }
    public string Stdout { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? ExpectedOutput { get; set; } // Added for LeetCode-style comparison
    public string? Error { get; set; }
    public decimal Runtime { get; set; }
    public long Memory { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Input { get; set; } // Added to show input in results
}

