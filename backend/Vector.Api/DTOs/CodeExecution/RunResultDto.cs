namespace Vector.Api.DTOs.CodeExecution;

/// <summary>
/// DTO for complete run result with all test cases
/// </summary>
public class RunResultDto
{
    public string Status { get; set; } = string.Empty; // ACCEPTED, WRONG_ANSWER, RUNTIME_ERROR, TLE, INVALID_INPUT
    public decimal? RuntimeMs { get; set; }
    public decimal? MemoryMb { get; set; }
    public List<CaseResultDto> Cases { get; set; } = new();
    public TestCaseParseErrorDto? ValidationError { get; set; }
}

public class TestCaseParseErrorDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
}

