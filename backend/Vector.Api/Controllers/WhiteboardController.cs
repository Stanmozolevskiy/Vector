using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Whiteboard;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhiteboardController : ControllerBase
{
    private readonly IWhiteboardService _whiteboardService;
    private readonly ILogger<WhiteboardController> _logger;

    public WhiteboardController(IWhiteboardService whiteboardService, ILogger<WhiteboardController> logger)
    {
        _whiteboardService = whiteboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get whiteboard data for the current user
    /// </summary>
    /// <param name="questionId">Optional question ID to get whiteboard for a specific question</param>
    /// <returns>Whiteboard data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WhiteboardDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWhiteboardData([FromQuery] string? questionId = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user ID" });
            }

            Guid? parsedQuestionId = string.IsNullOrWhiteSpace(questionId) 
                ? null 
                : Guid.TryParse(questionId, out var parsed) ? parsed : null;

            var whiteboardData = await _whiteboardService.GetWhiteboardDataAsync(userId, parsedQuestionId);

            if (whiteboardData == null)
            {
                return NotFound(new { error = "Whiteboard data not found" });
            }

            var dto = new WhiteboardDataDto
            {
                Id = whiteboardData.Id.ToString(),
                QuestionId = whiteboardData.QuestionId?.ToString(),
                Elements = whiteboardData.Elements,
                AppState = whiteboardData.AppState,
                Files = whiteboardData.Files,
                CreatedAt = whiteboardData.CreatedAt,
                UpdatedAt = whiteboardData.UpdatedAt,
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting whiteboard data");
            return StatusCode(500, new { error = "An error occurred while retrieving whiteboard data" });
        }
    }

    /// <summary>
    /// Save whiteboard data for the current user
    /// </summary>
    /// <param name="dto">Whiteboard data to save</param>
    /// <returns>Saved whiteboard data</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WhiteboardDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveWhiteboardData([FromBody] SaveWhiteboardDataDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user ID" });
            }

            var whiteboardData = await _whiteboardService.SaveWhiteboardDataAsync(userId, dto);

            var responseDto = new WhiteboardDataDto
            {
                Id = whiteboardData.Id.ToString(),
                QuestionId = whiteboardData.QuestionId?.ToString(),
                Elements = whiteboardData.Elements,
                AppState = whiteboardData.AppState,
                Files = whiteboardData.Files,
                CreatedAt = whiteboardData.CreatedAt,
                UpdatedAt = whiteboardData.UpdatedAt,
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving whiteboard data");
            return StatusCode(500, new { error = "An error occurred while saving whiteboard data" });
        }
    }

    /// <summary>
    /// Delete whiteboard data for the current user
    /// </summary>
    /// <param name="questionId">Optional question ID to delete whiteboard for a specific question</param>
    /// <returns>Success status</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWhiteboardData([FromQuery] string? questionId = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user ID" });
            }

            Guid? parsedQuestionId = string.IsNullOrWhiteSpace(questionId) 
                ? null 
                : Guid.TryParse(questionId, out var parsed) ? parsed : null;

            var deleted = await _whiteboardService.DeleteWhiteboardDataAsync(userId, parsedQuestionId);

            if (!deleted)
            {
                return NotFound(new { error = "Whiteboard data not found" });
            }

            return Ok(new { message = "Whiteboard data deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting whiteboard data");
            return StatusCode(500, new { error = "An error occurred while deleting whiteboard data" });
        }
    }
}
