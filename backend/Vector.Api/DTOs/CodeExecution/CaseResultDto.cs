namespace Vector.Api.DTOs.CodeExecution;

/// <summary>
/// DTO for a single test case result
/// </summary>
public class CaseResultDto
{
    public int CaseIndex { get; set; } // 1-based
    public object[] InputValues { get; set; } = Array.Empty<object>();
    public string[] ParameterNames { get; set; } = Array.Empty<string>();
    public string Stdout { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? ExpectedOutput { get; set; }
    public bool? Passed { get; set; }
    public decimal Runtime { get; set; }
    public long Memory { get; set; }
    public string Status { get; set; } = string.Empty;
    public RuntimeErrorDto? Error { get; set; }
}

public class RuntimeErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
}

