using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Hubs;
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
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public PeerInterviewController(
        IPeerInterviewService peerInterviewService,
        ILogger<PeerInterviewController> logger,
        IHubContext<CollaborationHub> hubContext,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _peerInterviewService = peerInterviewService;
        _logger = logger;
        _hubContext = hubContext;
        _configuration = configuration;
        _emailService = emailService;
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

    private static string GetRedirectUrlForInterviewType(string interviewType, Guid liveSessionId, Guid? activeQuestionId)
    {
        var t = (interviewType ?? string.Empty).Trim().ToLowerInvariant();
        if (t == "system-design")
        {
            return $"/system-design-interview/{liveSessionId}";
        }

        if (t == "behavioral" || t == "product-management")
        {
            return $"/peer-interviews/sessions/{liveSessionId}?type={Uri.EscapeDataString(t)}";
        }

        // Coding / SQL (QuestionDetailPage)
        if (activeQuestionId.HasValue)
        {
            return $"/questions/{activeQuestionId.Value}?session={liveSessionId}";
        }
        return $"/questions?session={liveSessionId}";
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
            if (session == null)
            {
                return BadRequest(new { error = "Cannot schedule interview session in the past." });
            }
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
    /// Get past scheduled sessions for the current user
    /// </summary>
    [HttpGet("scheduled/past")]
    [ProducesResponseType(typeof(IEnumerable<ScheduledInterviewSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPastSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var sessions = await _peerInterviewService.GetPastSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting past sessions");
            return StatusCode(500, new { message = "Failed to get past sessions", error = ex.Message });
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
            
            // Send SignalR notification to other participants
            // Note: We can't use Context.ConnectionId in a controller, so we send to all in the group
            // The frontend will handle ignoring its own updates
            var groupName = sessionId.ToString();
            await _hubContext.Clients.Group(groupName).SendAsync("RoleSwitched", new
            {
                sessionId = sessionId.ToString(),
                newActiveQuestionId = response.Session.ActiveQuestionId?.ToString()
            });
            
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
            
            // Send SignalR notification to other participants
            // Note: We can't use Context.ConnectionId in a controller, so we send to all in the group
            // The frontend will handle ignoring its own updates
            var groupName = sessionId.ToString();
            await _hubContext.Clients.Group(groupName).SendAsync("QuestionChanged", new
            {
                sessionId = sessionId.ToString(),
                questionId = response.Session.ActiveQuestionId?.ToString() ?? response.NewActiveQuestion?.Id.ToString()
            });
            
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
            
            // Send SignalR notification to other participants
            var groupName = sessionId.ToString();
            await _hubContext.Clients.Group(groupName).SendAsync("InterviewEnded", new
            {
                sessionId = sessionId.ToString()
            });
            
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

    /// <summary>
    /// Get feedback status for a session (check if user and opponent have submitted feedback)
    /// </summary>
    [HttpGet("sessions/{sessionId}/feedback-status")]
    [ProducesResponseType(typeof(FeedbackStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedbackStatus(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var status = await _peerInterviewService.GetFeedbackStatusAsync(sessionId, userId);
            return Ok(status);
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
            _logger.LogError(ex, "Error getting feedback status");
            return StatusCode(500, new { message = "Failed to get feedback status", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a "practice with a friend" interview immediately (no schedule / no queue).
    /// </summary>
    [HttpPost("friend")]
    [ProducesResponseType(typeof(FriendInterviewCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFriendInterview([FromBody] CreateFriendInterviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var (liveSessionId, creatorScheduledSessionId, interviewType, activeQuestionId) =
                await _peerInterviewService.CreateFriendInterviewAsync(userId, dto);

            var redirectUrl = GetRedirectUrlForInterviewType(interviewType, liveSessionId, activeQuestionId);
            var frontendUrl = (_configuration["Frontend:Url"] ?? "http://localhost:3000").TrimEnd('/');
            // Use a dedicated invite landing page so unauthenticated users don't get stuck in auth loops.
            // The landing page will prompt login (returnUrl) and auto-join the session after authentication.
            var inviteUrl = $"{frontendUrl}/friend-invite/{liveSessionId}";

            var emailSent = false;
            var emailDeliveryMode = "disabled";
            var partnerEmail = (dto.PartnerEmail ?? string.Empty).Trim();

            // Only attempt delivery when SendGrid is configured (avoid claiming "sent" when disabled).
            var sendGridApiKey = _configuration["SendGrid:ApiKey"]
                ?? Environment.GetEnvironmentVariable("SendGrid__ApiKey")
                ?? _configuration["SendGrid__ApiKey"];
            var isSendGridEnabled =
                !string.IsNullOrWhiteSpace(sendGridApiKey)
                && sendGridApiKey != "your_sendgrid_api_key"
                && sendGridApiKey != "your_sendgrid_api_key_here";

            if (isSendGridEnabled)
            {
                emailDeliveryMode = "sendgrid";
            }

            if (!string.IsNullOrWhiteSpace(partnerEmail) && isSendGridEnabled)
            {
                try
                {
                    var subject = "Vector interview invite: practice with a friend";
                    var bodyHtml = $@"
<h2>You've been invited to practice an interview on Vector</h2>
<p><strong>Interview type:</strong> {System.Net.WebUtility.HtmlEncode(interviewType)}</p>
<p>Click the link below to join the session:</p>
<p><a href=""{inviteUrl}"">Join the interview session</a></p>
<p>If the link doesn't work, copy and paste this URL into your browser:</p>
<p>{System.Net.WebUtility.HtmlEncode(inviteUrl)}</p>
";

                    await _emailService.SendEmailAsync(partnerEmail, subject, bodyHtml);
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    // Don't fail creation if email sending fails
                    _logger.LogError(ex, "Failed to send friend invite email to {Email}", partnerEmail);
                }
            }

            var response = new FriendInterviewCreatedDto
            {
                LiveSessionId = liveSessionId,
                CreatorScheduledSessionId = creatorScheduledSessionId,
                InterviewType = interviewType,
                ActiveQuestionId = activeQuestionId,
                RedirectUrl = redirectUrl,
                InviteUrl = inviteUrl,
                EmailSent = emailSent,
                EmailDeliveryMode = emailDeliveryMode
            };

            return StatusCode(201, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating friend interview");
            return StatusCode(500, new { message = "Failed to create friend interview", error = ex.Message });
        }
    }

    /// <summary>
    /// Join a "practice with a friend" live session (by link).
    /// When the second user joins, the system creates the matching records so the session shows up in both users' history.
    /// </summary>
    [HttpPost("friend/sessions/{liveSessionId}/join")]
    [ProducesResponseType(typeof(JoinFriendInterviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinFriendInterview(Guid liveSessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.JoinFriendInterviewAsync(liveSessionId, userId);
            return Ok(new JoinFriendInterviewResponseDto
            {
                Joined = true,
                Session = session
            });
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
            _logger.LogError(ex, "Error joining friend interview");
            return StatusCode(500, new { message = "Failed to join friend interview", error = ex.Message });
        }
    }
}

