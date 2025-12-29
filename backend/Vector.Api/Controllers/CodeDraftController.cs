using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Solution;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/code-drafts")]
[Authorize]
public class CodeDraftController : ControllerBase
{
    private readonly ICodeDraftService _codeDraftService;
    private readonly ILogger<CodeDraftController> _logger;

    public CodeDraftController(
        ICodeDraftService codeDraftService,
        ILogger<CodeDraftController> logger)
    {
        _codeDraftService = codeDraftService;
        _logger = logger;
    }

    /// <summary>
    /// Get code draft for a question and language
    /// </summary>
    [HttpGet("{questionId}/{language}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCodeDraft(Guid questionId, string language)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var draft = await _codeDraftService.GetCodeDraftAsync(userId.Value, questionId, language);
            if (draft == null)
            {
                return NoContent(); // Return 204 No Content instead of 404
            }

            return Ok(draft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting code draft");
            return StatusCode(500, new { error = "An error occurred while retrieving the code draft." });
        }
    }

    /// <summary>
    /// Save code draft
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveCodeDraft([FromBody] SaveCodeDraftDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var draft = await _codeDraftService.SaveCodeDraftAsync(userId.Value, dto);
            return Ok(draft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving code draft");
            return StatusCode(500, new { error = "An error occurred while saving the code draft." });
        }
    }

    /// <summary>
    /// Delete code draft
    /// </summary>
    [HttpDelete("{questionId}/{language}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCodeDraft(Guid questionId, string language)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User not authenticated." });
            }

            var deleted = await _codeDraftService.DeleteCodeDraftAsync(userId.Value, questionId, language);
            if (!deleted)
            {
                return NotFound(new { error = "Code draft not found." });
            }

            return Ok(new { message = "Code draft deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting code draft");
            return StatusCode(500, new { error = "An error occurred while deleting the code draft." });
        }
    }

    private Guid? GetCurrentUserId()
    {
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

