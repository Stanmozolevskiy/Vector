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
    /// Validate code against all test cases for a question
    /// </summary>
    Task<TestResultDto[]> ValidateSolutionAsync(Guid questionId, ExecutionRequestDto request);

    /// <summary>
    /// Get list of supported programming languages
    /// </summary>
    Task<List<SupportedLanguageDto>> GetSupportedLanguagesAsync();
}

