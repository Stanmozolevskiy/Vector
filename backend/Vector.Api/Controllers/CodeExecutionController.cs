using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.DTOs.CodeExecution;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CodeExecutionController : ControllerBase
{
    private readonly ICodeExecutionService _codeExecutionService;
    private readonly ILogger<CodeExecutionController> _logger;

    public CodeExecutionController(
        ICodeExecutionService codeExecutionService,
        ILogger<CodeExecutionController> logger)
    {
        _codeExecutionService = codeExecutionService;
        _logger = logger;
    }

    /// <summary>
    /// Execute code with optional input
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteCode([FromBody] ExecutionRequestDto request)
    {
        try
        {
            var result = await _codeExecutionService.ExecuteCodeAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing code");
            return StatusCode(500, new { error = "An error occurred while executing the code." });
        }
    }

    /// <summary>
    /// Validate code against all test cases for a question
    /// </summary>
    [HttpPost("validate/{questionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateSolution(Guid questionId, [FromBody] ExecutionRequestDto request)
    {
        try
        {
            var results = await _codeExecutionService.ValidateSolutionAsync(questionId, request);
            return Ok(results);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Question not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating solution for question {QuestionId}", questionId);
            return StatusCode(500, new { error = "An error occurred while validating the solution." });
        }
    }

    /// <summary>
    /// Get list of supported programming languages
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupportedLanguages()
    {
        try
        {
            var languages = await _codeExecutionService.GetSupportedLanguagesAsync();
            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported languages");
            return StatusCode(500, new { error = "An error occurred while retrieving supported languages." });
        }
    }
}

