using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Solution;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/solutions")]
[Authorize]
public class SolutionController : ControllerBase
{
    private readonly ISolutionService _solutionService;
    private readonly ILogger<SolutionController> _logger;

    public SolutionController(
        ISolutionService solutionService,
        ILogger<SolutionController> logger)
    {
        _solutionService = solutionService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a solution for a question (only called when validation passes)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitSolution([FromBody] SubmitSolutionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            await _solutionService.SubmitSolutionAsync(userId.Value, dto);
            return Ok(new { message = "Solution submitted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Question not found for solution submission");
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during solution submission");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting solution");
            return StatusCode(500, new { error = "An error occurred while submitting the solution." });
        }
    }

    /// <summary>
    /// Get current user's solutions
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySolutions([FromQuery] SolutionFilterDto? filter)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var (solutions, totalCount) = await _solutionService.GetUserSolutionsAsync(userId.Value, filter);
            return Ok(new { solutions, totalCount, page = filter?.Page ?? 1, pageSize = filter?.PageSize ?? 20 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user solutions");
            return StatusCode(500, new { error = "An error occurred while retrieving solutions." });
        }
    }

    /// <summary>
    /// Get solution by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSolutionById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var solution = await _solutionService.GetSolutionByIdAsync(id, userId.Value);
            if (solution == null)
            {
                return NotFound(new { error = "Solution not found." });
            }

            return Ok(solution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting solution by ID");
            return StatusCode(500, new { error = "An error occurred while retrieving the solution." });
        }
    }

    /// <summary>
    /// Get solutions for a specific question
    /// </summary>
    [HttpGet("question/{questionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSolutionsForQuestion(Guid questionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var solutions = await _solutionService.GetSolutionsForQuestionAsync(questionId, userId.Value);
            return Ok(solutions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting solutions for question");
            return StatusCode(500, new { error = "An error occurred while retrieving solutions." });
        }
    }

    /// <summary>
    /// Check if user has solved a specific question (optimized lookup)
    /// </summary>
    [HttpGet("question/{questionId}/solved")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HasSolvedQuestion(Guid questionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var hasSolved = await _solutionService.HasUserSolvedQuestionAsync(userId.Value, questionId);
            return Ok(new { solved = hasSolved });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if question is solved");
            return StatusCode(500, new { error = "An error occurred while checking solved status." });
        }
    }

    /// <summary>
    /// Get solution statistics for current user
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var statistics = await _solutionService.GetSolutionStatisticsAsync(userId.Value);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting solution statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving statistics." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        // JWT tokens may use different claim names
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}

