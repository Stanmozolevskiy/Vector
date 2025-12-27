using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vector.Api.Models;
using Vector.Api.Services;
using Vector.Api.Data;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/peer-interviews")]
[Authorize]
public class PeerInterviewController : ControllerBase
{
    private readonly IPeerInterviewService _peerInterviewService;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PeerInterviewController> _logger;

    public PeerInterviewController(
        IPeerInterviewService peerInterviewService,
        IEmailService emailService,
        ApplicationDbContext context,
        ILogger<PeerInterviewController> logger)
    {
        _peerInterviewService = peerInterviewService;
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }

    [HttpPost("find-match")]
    public async Task<ActionResult<PeerInterviewMatch>> FindMatch([FromBody] FindMatchRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var match = await _peerInterviewService.FindMatchAsync(
                userId,
                request.PreferredDifficulty,
                request.PreferredCategories
            );

            if (match == null)
            {
                return NotFound(new { message = "No available peer match found" });
            }

            return Ok(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding peer match");
            return StatusCode(500, new { message = "An error occurred while finding a match" });
        }
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<PeerInterviewSession>> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Validate that user is either interviewer or interviewee
            if (request.InterviewerId != userId && request.IntervieweeId != userId)
            {
                return Forbid("You can only create sessions where you are the interviewer or interviewee");
            }

            var session = await _peerInterviewService.CreateSessionAsync(
                request.InterviewerId,
                request.IntervieweeId,
                request.QuestionId,
                request.ScheduledTime,
                request.Duration ?? 45,
                request.InterviewType,
                request.PracticeType,
                request.InterviewLevel
            );

            // Load full session with user details for email
            var fullSession = await _context.PeerInterviewSessions
                .Include(s => s.Interviewer)
                .Include(s => s.Interviewee)
                .Include(s => s.Question)
                .FirstOrDefaultAsync(s => s.Id == session.Id);

            // Send confirmation email
            if (fullSession != null)
            {
                try
                {
                    var interviewer = await _context.Users.FindAsync(fullSession.InterviewerId);
                    if (interviewer != null)
                    {
                        var scheduledDate = fullSession.ScheduledTime?.ToString("dddd, MMMM d, yyyy, h:mm tt") ?? "TBD";
                        var interviewType = fullSession.InterviewType ?? "Mock Interview";
                        var questionTitle = fullSession.Question?.Title ?? "TBD";
                        var questionLink = fullSession.QuestionId.HasValue 
                            ? $"https://localhost:3000/questions/{fullSession.QuestionId}"
                            : "#";

                        var emailBody = $@"
Hey {interviewer.FirstName ?? interviewer.Email?.Split('@')[0] ?? "there"},

Thanks for scheduling a mock interview on Vector! Here are the details of your interview:

When: {scheduledDate}
Interview Type: {interviewType}

<a href='#' style='color: #3b82f6; text-decoration: none;'>Add to Google Calendar</a>

Here is the question you'll be asking your peer: {questionTitle}

Remember that every interview is bi-directional. Both you and your peer will interview each other in the same practice session.

<a href='{questionLink}' style='display: inline-block; background: #7c3aed; color: white; padding: 0.75rem 1.5rem; border-radius: 6px; text-decoration: none; margin: 1rem 0;'>Read the question here</a>

Have more questions? Take a quick visit to our FAQ page. If you scheduled by mistake or can't make it, please cancel or reschedule your interview.

— The Vector Team
";

                        await _emailService.SendEmailAsync(
                            interviewer.Email,
                            $"Your Scheduled Mock Interview on {scheduledDate}",
                            emailBody
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send confirmation email for session {SessionId}", session.Id);
                    // Don't fail the request if email fails
                }
            }

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, fullSession ?? session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating interview session");
            return StatusCode(500, new { message = "An error occurred while creating the session" });
        }
    }

    [HttpGet("sessions/me")]
    public async Task<ActionResult<List<PeerInterviewSession>>> GetMySessions([FromQuery] string? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var sessions = await _peerInterviewService.GetUserSessionsAsync(userId, status);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user sessions");
            return StatusCode(500, new { message = "An error occurred while retrieving sessions" });
        }
    }

    [HttpGet("sessions/{id}")]
    public async Task<ActionResult<PeerInterviewSession>> GetSession(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.GetSessionByIdAsync(id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Verify user has access to this session
            if (session.InterviewerId != userId && session.IntervieweeId != userId)
            {
                return Forbid("You do not have access to this session");
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session");
            return StatusCode(500, new { message = "An error occurred while retrieving the session" });
        }
    }

    [HttpPut("sessions/{id}/status")]
    public async Task<ActionResult<PeerInterviewSession>> UpdateSessionStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.GetSessionByIdAsync(id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Verify user has access to this session
            if (session.InterviewerId != userId && session.IntervieweeId != userId)
            {
                return Forbid("You do not have access to this session");
            }

            var updatedSession = await _peerInterviewService.UpdateSessionStatusAsync(id, request.Status);
            return Ok(updatedSession);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session status");
            return StatusCode(500, new { message = "An error occurred while updating the session status" });
        }
    }

    [HttpPut("sessions/{id}/cancel")]
    public async Task<ActionResult> CancelSession(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var cancelled = await _peerInterviewService.CancelSessionAsync(id, userId);

            if (!cancelled)
            {
                return BadRequest(new { message = "Session could not be cancelled. It may not exist, you may not have permission, or it may not be in a cancellable state." });
            }

            return Ok(new { message = "Session cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling session");
            return StatusCode(500, new { message = "An error occurred while cancelling the session" });
        }
    }

    [HttpPut("match-preferences")]
    public async Task<ActionResult<PeerInterviewMatch>> UpdateMatchPreferences([FromBody] UpdateMatchPreferencesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var match = await _peerInterviewService.UpdateMatchPreferencesAsync(
                userId,
                request.PreferredDifficulty,
                request.PreferredCategories,
                request.Availability,
                request.IsAvailable
            );

            return Ok(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating match preferences");
            return StatusCode(500, new { message = "An error occurred while updating match preferences" });
        }
    }

    [HttpGet("match-preferences")]
    public async Task<ActionResult<PeerInterviewMatch>> GetMatchPreferences()
    {
        try
        {
            var userId = GetCurrentUserId();
            var match = await _peerInterviewService.GetMatchPreferencesAsync(userId);

            if (match == null)
            {
                return NotFound(new { message = "Match preferences not found" });
            }

            return Ok(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving match preferences");
            return StatusCode(500, new { message = "An error occurred while retrieving match preferences" });
        }
    }

    [HttpPost("sessions/{id}/change-question")]
    public async Task<ActionResult<PeerInterviewSession>> ChangeQuestion(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedSession = await _peerInterviewService.ChangeQuestionAsync(id, userId);

            // Load full session with question details
            var fullSession = await _context.PeerInterviewSessions
                .Include(s => s.Interviewer)
                .Include(s => s.Interviewee)
                .Include(s => s.Question)
                .FirstOrDefaultAsync(s => s.Id == updatedSession.Id);

            return Ok(fullSession);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing question");
            return StatusCode(500, new { message = "An error occurred while changing the question" });
        }
    }

    [HttpPost("sessions/{id}/switch-roles")]
    public async Task<ActionResult<PeerInterviewSession>> SwitchRoles(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedSession = await _peerInterviewService.SwitchRolesAsync(id, userId);

            // Load full session with question details
            var fullSession = await _context.PeerInterviewSessions
                .Include(s => s.Interviewer)
                .Include(s => s.Interviewee)
                .Include(s => s.Question)
                .FirstOrDefaultAsync(s => s.Id == updatedSession.Id);

            return Ok(fullSession);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Session not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching roles");
            return StatusCode(500, new { message = "An error occurred while switching roles" });
        }
    }
}

// DTOs
public class FindMatchRequest
{
    public string? PreferredDifficulty { get; set; }
    public List<string>? PreferredCategories { get; set; }
}

public class CreateSessionRequest
{
    public Guid InterviewerId { get; set; }
    public Guid IntervieweeId { get; set; }
    public Guid? QuestionId { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public int? Duration { get; set; }
    public string? InterviewType { get; set; }
    public string? PracticeType { get; set; }
    public string? InterviewLevel { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateMatchPreferencesRequest
{
    public string? PreferredDifficulty { get; set; }
    public List<string>? PreferredCategories { get; set; }
    public string? Availability { get; set; }
    public bool? IsAvailable { get; set; }
}

