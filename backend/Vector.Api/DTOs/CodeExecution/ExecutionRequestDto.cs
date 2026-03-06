using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.CodeExecution;

public class ExecutionRequestDto
{
    [Required]
    public string SourceCode { get; set; } = string.Empty;

    [Required]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Optional input data (stdin) for code execution
    /// </summary>
    public string? Stdin { get; set; }
}

