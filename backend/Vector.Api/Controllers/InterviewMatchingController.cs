using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

/// <summary>
/// Controller for interview matching functionality
/// Handles matching users for peer interviews
/// </summary>
[ApiController]
[Route("api/interview-matching")]
[Authorize]
public class InterviewMatchingController : ControllerBase
{
    private readonly IInterviewMatchingService _matchingService;
    private readonly ILogger<InterviewMatchingController> _logger;

    public InterviewMatchingController(
        IInterviewMatchingService matchingService,
        ILogger<InterviewMatchingController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value 
                        ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or expired authentication token");
        }

        return userId;
    }

    /// <summary>
    /// Start matching process for a scheduled session
    /// </summary>
    [HttpPost("sessions/{sessionId}/start")]
    [ProducesResponseType(typeof(StartMatchingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartMatching(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _matchingService.StartMatchingAsync(sessionId, userId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting matching");
            return StatusCode(500, new { message = "Failed to start matching", error = ex.Message });
        }
    }

    /// <summary>
    /// Get matching status for a scheduled session
    /// Returns 204 No Content if no matching request exists (this is a valid state)
    /// </summary>
    [HttpGet("sessions/{sessionId}/status")]
    [ProducesResponseType(typeof(MatchingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetMatchingStatus(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var status = await _matchingService.GetMatchingStatusAsync(sessionId, userId);
            if (status == null)
            {
                return NoContent();
            }
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matching status");
            return StatusCode(500, new { message = "Failed to get matching status", error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm a match (user confirms readiness)
    /// </summary>
    [HttpPost("matching-requests/{matchingRequestId}/confirm")]
    [ProducesResponseType(typeof(ConfirmMatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmMatch(Guid matchingRequestId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _matchingService.ConfirmMatchAsync(matchingRequestId, userId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming match");
            return StatusCode(500, new { message = "Failed to confirm match", error = ex.Message });
        }
    }

    /// <summary>
    /// Expire a match if not both confirmed within 15 seconds (re-queue users)
    /// </summary>
    [HttpPost("matching-requests/{matchingRequestId}/expire")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExpireMatch(Guid matchingRequestId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var expired = await _matchingService.ExpireMatchIfNotConfirmedAsync(matchingRequestId, userId);
            if (!expired)
            {
                return Ok(new { message = "Match not expired (either already confirmed or not ready to expire)" });
            }
            return Ok(new { message = "Match expired, users re-queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring match");
            return StatusCode(500, new { message = "Failed to expire match", error = ex.Message });
        }
    }

    /// <summary>
    /// Expire all matching requests for a session (used on page refresh/close)
    /// Does not create new requests
    /// </summary>
    [HttpPost("sessions/{sessionId}/expire-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExpireAllRequests(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _matchingService.ExpireAllRequestsForSessionAsync(sessionId, userId);
            return Ok(new { message = "All matching requests expired" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring all requests for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Failed to expire requests", error = ex.Message });
        }
    }
}
