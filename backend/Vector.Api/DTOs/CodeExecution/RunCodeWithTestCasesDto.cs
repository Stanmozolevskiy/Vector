using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.CodeExecution;

/// <summary>
/// DTO for running code with line-based testcase input
/// </summary>
public class RunCodeWithTestCasesDto
{
    [Required]
    public string SourceCode { get; set; } = string.Empty;

    [Required]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Raw text from testcase editor (line-based format)
    /// </summary>
    [Required]
    public string TestCaseText { get; set; } = string.Empty;
}

