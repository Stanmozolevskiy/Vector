using Vector.Api.DTOs.CodeExecution;

namespace Vector.Api.Services;

/// <summary>
/// Service interface for code execution using Judge0 API
/// </summary>
public interface ICodeExecutionService
{
    /// <summary>
    /// Execute code with optional input
    /// </summary>
    Task<ExecutionResultDto> ExecuteCodeAsync(ExecutionRequestDto request);

    /// <summary>
    /// Run code against visible (non-hidden) test cases for a question (for "Run" button)
    /// </summary>
    Task<TestResultDto[]> RunCodeAsync(Guid questionId, ExecutionRequestDto request);

    /// <summary>
    /// Run code with line-based testcase input (new UI format)
    /// </summary>
    Task<RunResultDto> RunCodeWithTestCasesAsync(Guid questionId, RunCodeWithTestCasesDto request);

    /// <summary>
    /// Validate code against all test cases for a question, including hidden ones (for "Submit" button)
    /// </summary>
    Task<TestResultDto[]> ValidateSolutionAsync(Guid questionId, ExecutionRequestDto request);

    /// <summary>
    /// Get list of supported programming languages
    /// </summary>
    Task<List<SupportedLanguageDto>> GetSupportedLanguagesAsync();
}

