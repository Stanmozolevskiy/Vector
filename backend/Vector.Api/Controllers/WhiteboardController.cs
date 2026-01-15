using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vector.Api.Data;
using Vector.Api.DTOs.Whiteboard;
using Vector.Api.Models;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhiteboardController : ControllerBase
{
    private readonly IWhiteboardService _whiteboardService;
    private readonly ILogger<WhiteboardController> _logger;
    private readonly ApplicationDbContext _context;

    public WhiteboardController(IWhiteboardService whiteboardService, ILogger<WhiteboardController> logger, ApplicationDbContext context)
    {
        _whiteboardService = whiteboardService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get whiteboard data for the current user or partner
    /// </summary>
    /// <param name="questionId">Optional question ID to get whiteboard for a specific question</param>
    /// <param name="sessionId">Optional session ID to get whiteboard for a session</param>
    /// <param name="partnerUserId">Optional partner user ID to get their whiteboard</param>
    /// <returns>Whiteboard data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WhiteboardDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWhiteboardData([FromQuery] string? questionId = null, [FromQuery] string? sessionId = null, [FromQuery] string? partnerUserId = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user ID" });
            }

            // If sessionId is provided, get session-based whiteboard (shared between users)
            if (!string.IsNullOrWhiteSpace(sessionId) && Guid.TryParse(sessionId, out var parsedSessionId))
            {
                // Ensure current user is a participant in the live session
                var isParticipant = await _context.LiveInterviewParticipants
                    .AsNoTracking()
                    .AnyAsync(p => p.LiveSessionId == parsedSessionId && p.UserId == userId);

                if (!isParticipant)
                {
                    return Forbid();
                }

                var whiteboardData = await _whiteboardService.GetWhiteboardDataBySessionAsync(parsedSessionId);
                
                if (whiteboardData == null)
                {
                    // Return empty board instead of 404 to make first-load UX smooth
                    return Ok(new WhiteboardDataDto
                    {
                        Id = string.Empty,
                        QuestionId = null,
                        Elements = "[]",
                        AppState = "{}",
                        Files = "{}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    });
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

            // Check if questionId is a session-based identifier (starts with "session-")
            if (!string.IsNullOrWhiteSpace(questionId) && questionId.StartsWith("session-", StringComparison.OrdinalIgnoreCase))
            {
                var sessionIdStr = questionId.Substring("session-".Length);
                if (Guid.TryParse(sessionIdStr, out var sessionIdFromQuestion))
                {
                    // If partnerUserId is provided, get partner's whiteboard for this session
                    if (!string.IsNullOrWhiteSpace(partnerUserId) && Guid.TryParse(partnerUserId, out var partnerId))
                    {
                        var partnerWhiteboardData = await _whiteboardService.GetWhiteboardDataBySessionAndUserAsync(sessionIdFromQuestion, partnerId);
                        
                        if (partnerWhiteboardData == null)
                        {
                            return NotFound(new { error = "Partner whiteboard data not found for session" });
                        }

                        var partnerDto = new WhiteboardDataDto
                        {
                            Id = partnerWhiteboardData.Id.ToString(),
                            QuestionId = partnerWhiteboardData.QuestionId?.ToString(),
                            Elements = partnerWhiteboardData.Elements,
                            AppState = partnerWhiteboardData.AppState,
                            Files = partnerWhiteboardData.Files,
                            CreatedAt = partnerWhiteboardData.CreatedAt,
                            UpdatedAt = partnerWhiteboardData.UpdatedAt,
                        };

                        return Ok(partnerDto);
                    }
                    
                    // Otherwise, get session-based whiteboard (shared)
                    var whiteboardData = await _whiteboardService.GetWhiteboardDataBySessionAsync(sessionIdFromQuestion);
                    
                    if (whiteboardData == null)
                    {
                        return NotFound(new { error = "Whiteboard data not found for session" });
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
            }

            // Otherwise, get user-based whiteboard
            Guid? parsedQuestionId = string.IsNullOrWhiteSpace(questionId) 
                ? null 
                : Guid.TryParse(questionId, out var parsed) ? parsed : null;

            var userWhiteboardData = await _whiteboardService.GetWhiteboardDataAsync(userId, parsedQuestionId);

            if (userWhiteboardData == null)
            {
                return NotFound(new { error = "Whiteboard data not found" });
            }

            var userDto = new WhiteboardDataDto
            {
                Id = userWhiteboardData.Id.ToString(),
                QuestionId = userWhiteboardData.QuestionId?.ToString(),
                Elements = userWhiteboardData.Elements,
                AppState = userWhiteboardData.AppState,
                Files = userWhiteboardData.Files,
                CreatedAt = userWhiteboardData.CreatedAt,
                UpdatedAt = userWhiteboardData.UpdatedAt,
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting whiteboard data");
            return StatusCode(500, new { error = "An error occurred while retrieving whiteboard data" });
        }
    }

    /// <summary>
    /// Save whiteboard data for the current user or specified user
    /// </summary>
    /// <param name="dto">Whiteboard data to save</param>
    /// <param name="userId">Optional user ID to save to (for role-based whiteboards)</param>
    /// <returns>Saved whiteboard data</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WhiteboardDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveWhiteboardData([FromBody] SaveWhiteboardDataDto dto, [FromQuery] string? userId = null, [FromQuery] string? sessionId = null)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "Invalid user ID" });
            }

            // Use provided userId if specified, otherwise use current user's ID
            Guid targetUserId = currentUserId;
            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var parsedUserId))
            {
                targetUserId = parsedUserId;
            }

            // If sessionId is provided, save shared session whiteboard (not user-owned)
            if (!string.IsNullOrWhiteSpace(sessionId) && Guid.TryParse(sessionId, out var parsedSessionId))
            {
                var isParticipant = await _context.LiveInterviewParticipants
                    .AsNoTracking()
                    .AnyAsync(p => p.LiveSessionId == parsedSessionId && p.UserId == currentUserId);

                if (!isParticipant)
                {
                    return Forbid();
                }

                var sessionWhiteboard = await _whiteboardService.SaveWhiteboardDataBySessionAsync(parsedSessionId, dto);
                return Ok(new WhiteboardDataDto
                {
                    Id = sessionWhiteboard.Id.ToString(),
                    QuestionId = sessionWhiteboard.QuestionId?.ToString(),
                    Elements = sessionWhiteboard.Elements,
                    AppState = sessionWhiteboard.AppState,
                    Files = sessionWhiteboard.Files,
                    CreatedAt = sessionWhiteboard.CreatedAt,
                    UpdatedAt = sessionWhiteboard.UpdatedAt,
                });
            }

            // Check if this is a session-based whiteboard (questionId starts with "session-")
            WhiteboardData whiteboardData;
            if (!string.IsNullOrWhiteSpace(dto.QuestionId) && dto.QuestionId.StartsWith("session-", StringComparison.OrdinalIgnoreCase))
            {
                var sessionIdStr = dto.QuestionId.Substring("session-".Length);
                if (Guid.TryParse(sessionIdStr, out var parsedSessionIdFromQuestionId))
                {
                    // Save to user-specific whiteboard for this session
                    whiteboardData = await _whiteboardService.SaveWhiteboardDataAsync(targetUserId, dto);
                }
                else
                {
                    return BadRequest(new { error = "Invalid session ID format" });
                }
            }
            else
            {
                whiteboardData = await _whiteboardService.SaveWhiteboardDataAsync(targetUserId, dto);
            }

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
