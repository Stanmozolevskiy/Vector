using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/peer-interviews")]
[Authorize]
public class PeerInterviewController : ControllerBase
{
    private readonly IPeerInterviewService _peerInterviewService;
    private readonly ILogger<PeerInterviewController> _logger;
    private readonly IHubContext<CollaborationHub> _hubContext;

    public PeerInterviewController(
        IPeerInterviewService peerInterviewService,
        ILogger<PeerInterviewController> logger)
    {
        _peerInterviewService = peerInterviewService;
        _logger = logger;
        _hubContext = hubContext;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Schedule a new interview session
    /// </summary>
    [HttpPost("scheduled")]
    [ProducesResponseType(typeof(ScheduledInterviewSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.ScheduleInterviewSessionAsync(userId, dto);
            return StatusCode(201, session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling interview session");
            return StatusCode(500, new { message = "Failed to schedule interview session", error = ex.Message });
        }
    }

    /// <summary>
    /// Get upcoming scheduled sessions for the current user
    /// </summary>
    [HttpGet("scheduled/upcoming")]
    [ProducesResponseType(typeof(IEnumerable<ScheduledInterviewSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var sessions = await _peerInterviewService.GetUpcomingSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming sessions");
            return StatusCode(500, new { message = "Failed to get upcoming sessions", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a scheduled session by ID
    /// </summary>
    [HttpGet("scheduled/{sessionId}")]
    [ProducesResponseType(typeof(ScheduledInterviewSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduledSession(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.GetScheduledSessionByIdAsync(sessionId, userId);
            if (session == null)
            {
                return NotFound(new { message = "Scheduled session not found" });
            }
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduled session");
            return StatusCode(500, new { message = "Failed to get scheduled session", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a scheduled session
    /// </summary>
    [HttpPost("scheduled/{sessionId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelScheduledSession(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var cancelled = await _peerInterviewService.CancelScheduledSessionAsync(sessionId, userId);
            if (!cancelled)
            {
                return NotFound(new { message = "Scheduled session not found" });
            }
            return Ok(new { message = "Session cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling scheduled session");
            return StatusCode(500, new { message = "Failed to cancel scheduled session", error = ex.Message });
        }
    }

    /// <summary>
    /// Start matching process for a scheduled session
    /// </summary>
    [HttpPost("sessions/{sessionId}/start-matching")]
    [ProducesResponseType(typeof(StartMatchingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartMatching(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _peerInterviewService.StartMatchingAsync(sessionId, userId);
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
    [HttpGet("sessions/{sessionId}/matching-status")]
    [ProducesResponseType(typeof(MatchingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetMatchingStatus(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var status = await _peerInterviewService.GetMatchingStatusAsync(sessionId, userId);
            if (status == null)
            {
                // Return 204 No Content instead of 404 - this is a valid state (no matching request yet)
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
    public async Task<IActionResult> ConfirmMatch(Guid matchingRequestId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _peerInterviewService.ConfirmMatchAsync(matchingRequestId, userId);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExpireMatch(Guid matchingRequestId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var expired = await _peerInterviewService.ExpireMatchIfNotConfirmedAsync(matchingRequestId, userId);
            if (!expired)
            {
                return Ok(new { message = "Match not expired (either already confirmed or not ready to expire)" });
            }
            return Ok(new { message = "Match expired, users re-queued" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring match");
            return StatusCode(500, new { message = "Failed to expire match", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a live interview session by ID, or a scheduled session if no live session exists
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    [ProducesResponseType(typeof(LiveInterviewSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ScheduledInterviewSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // First try to get as live session
            var liveSession = await _peerInterviewService.GetLiveSessionByIdAsync(sessionId, userId);
            if (liveSession != null)
            {
                return Ok(liveSession);
            }
            
            // If not found as live session, try as scheduled session
            var scheduledSession = await _peerInterviewService.GetScheduledSessionByIdAsync(sessionId, userId);
            if (scheduledSession != null)
            {
                return Ok(scheduledSession);
            }
            
            return NotFound(new { message = "Session not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session");
            return StatusCode(500, new { message = "Failed to get session", error = ex.Message });
        }
    }

    /// <summary>
    /// Switch roles between interviewer and interviewee
    /// </summary>
    [HttpPost("sessions/{sessionId}/switch-roles")]
    [ProducesResponseType(typeof(SwitchRolesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SwitchRoles(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _peerInterviewService.SwitchRolesAsync(sessionId, userId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching roles");
            return StatusCode(500, new { message = "Failed to switch roles", error = ex.Message });
        }
    }

    /// <summary>
    /// Change the active question in a session (interviewer only)
    /// </summary>
    [HttpPost("sessions/{sessionId}/change-question")]
    [ProducesResponseType(typeof(ChangeQuestionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeQuestion(
        Guid sessionId,
        [FromBody] ChangeQuestionRequestDto? dto = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var newQuestionId = dto?.QuestionId;
            var response = await _peerInterviewService.ChangeQuestionAsync(sessionId, userId, newQuestionId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing question");
            return StatusCode(500, new { message = "Failed to change question", error = ex.Message });
        }
    }

    /// <summary>
    /// End an interview session
    /// </summary>
    [HttpPost("sessions/{sessionId}/end")]
    [ProducesResponseType(typeof(LiveInterviewSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndInterview(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.EndInterviewAsync(sessionId, userId);
            return Ok(session);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending interview");
            return StatusCode(500, new { message = "Failed to end interview", error = ex.Message });
        }
    }

    /// <summary>
    /// Submit feedback for a session
    /// </summary>
    [HttpPost("feedback")]
    [ProducesResponseType(typeof(InterviewFeedbackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var feedback = await _peerInterviewService.SubmitFeedbackAsync(userId, dto);
            return StatusCode(201, feedback);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            return StatusCode(500, new { message = "Failed to submit feedback", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all feedback for a session
    /// </summary>
    [HttpGet("sessions/{sessionId}/feedback")]
    [ProducesResponseType(typeof(IEnumerable<InterviewFeedbackDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionFeedback(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var feedbacks = await _peerInterviewService.GetFeedbackForSessionAsync(sessionId, userId);
            return Ok(feedbacks);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session feedback");
            return StatusCode(500, new { message = "Failed to get session feedback", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific feedback by ID
    /// </summary>
    [HttpGet("feedback/{feedbackId}")]
    [ProducesResponseType(typeof(InterviewFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedback(Guid feedbackId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var feedback = await _peerInterviewService.GetFeedbackAsync(feedbackId, userId);
            if (feedback == null)
            {
                return NotFound(new { message = "Feedback not found" });
            }
            return Ok(feedback);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback");
            return StatusCode(500, new { message = "Failed to get feedback", error = ex.Message });
        }
    }
}

