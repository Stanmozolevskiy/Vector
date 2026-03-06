namespace Vector.Api.DTOs.CodeExecution;

public class ExecutionResultDto
{
    public string Output { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string Status { get; set; } = string.Empty; // Accepted, Wrong Answer, Time Limit Exceeded, etc.
    public decimal Runtime { get; set; } // in seconds
    public long Memory { get; set; } // in bytes
    public string? CompileOutput { get; set; }
}

