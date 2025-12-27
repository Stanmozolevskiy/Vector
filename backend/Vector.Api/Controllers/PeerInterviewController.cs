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

            // Validate that user is either the interviewer or the interviewee
            // This allows users to create sessions where they are the interviewee
            if (request.InterviewerId != userId && request.IntervieweeId != userId)
            {
                return Forbid("You can only create sessions where you are the interviewer or interviewee");
            }

            var session = await _peerInterviewService.CreateSessionAsync(
                request.InterviewerId,
                request.IntervieweeId, // Can be null for matching pending
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

    [HttpPost("sessions/{id}/start-matching")]
    public async Task<ActionResult> StartMatching(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // First check if session already has an interviewee
            var session = await _peerInterviewService.GetSessionByIdAsync(id);
            if (session != null && session.IntervieweeId.HasValue)
            {
                // Session is already complete, return success with indication to navigate
                return Ok(new { 
                    matchingRequest = new { 
                        status = "Completed",
                        sessionId = id.ToString()
                    }, 
                    matched = true,
                    sessionComplete = true
                });
            }
            
            var matchingRequest = await _peerInterviewService.CreateMatchingRequestAsync(id, userId);
            
            // If request is already matched or confirmed, return it immediately
            if (matchingRequest.Status == "Matched" || matchingRequest.Status == "Confirmed")
            {
                return Ok(new { matchingRequest = matchingRequest, matched = true });
            }
            
            // Try to find a match immediately (only if not already matched)
            var match = await _peerInterviewService.FindMatchingPeerAsync(userId, id);
            
            if (match != null)
            {
                return Ok(new { matchingRequest = match, matched = true });
            }

            return Ok(new { matchingRequest = matchingRequest, matched = false });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // If session already has interviewee, return success to allow navigation
            if (ex.Message.Contains("already has an interviewee"))
            {
                try
                {
                    var session = await _peerInterviewService.GetSessionByIdAsync(id);
                    if (session != null && session.IntervieweeId.HasValue)
                    {
                        return Ok(new { 
                            matchingRequest = new { 
                                status = "Completed",
                                sessionId = id.ToString()
                            }, 
                            matched = true,
                            sessionComplete = true
                        });
                    }
                }
                catch
                {
                    // Fall through to return error
                }
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting matching");
            return StatusCode(500, new { message = "An error occurred while starting matching" });
        }
    }

    [HttpGet("sessions/{id}/matching-status")]
    public async Task<ActionResult> GetMatchingStatus(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _peerInterviewService.GetSessionByIdAsync(id);
            
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // User must be either the interviewer or interviewee of this session
            // OR user must be part of a matching request for this session
            var isInterviewer = session.InterviewerId == userId;
            var isInterviewee = session.IntervieweeId.HasValue && session.IntervieweeId == userId;
            
            // Check if user is part of a matching request for this session (before denying access)
            var userInMatchingRequest = await _context.InterviewMatchingRequests
                .AnyAsync(r => r.ScheduledSessionId == id && (r.UserId == userId || r.MatchedUserId == userId));
            
            if (!isInterviewer && !isInterviewee && !userInMatchingRequest)
            {
                // Also check if user is the matched user in a request that points to this session
                var userIsMatchedInRequest = await _context.InterviewMatchingRequests
                    .Include(r => r.MatchedRequest)
                    .AnyAsync(r => 
                        r.MatchedUserId == userId && 
                        r.MatchedRequest != null && 
                        r.MatchedRequest.ScheduledSessionId == id);
                
                if (!userIsMatchedInRequest)
                {
                    return Forbid("You do not have access to this session");
                }
            }

            // Find matching request for this session (where this session is the scheduled session)
            var matchingRequest = await _context.InterviewMatchingRequests
                .Include(r => r.MatchedRequest)
                    .ThenInclude(r => r!.User)
                .Include(r => r.MatchedRequest)
                    .ThenInclude(r => r!.ScheduledSession)
                .FirstOrDefaultAsync(r => r.ScheduledSessionId == id);

            // Also check if user is part of a matched request (as the matched user)
            // This handles the case where User 2's session is matched with User 1's session
            // We need to find a request where:
            // 1. This user is the MatchedUserId in a request
            // 2. That request's MatchedRequest points to a request for this session
            if (matchingRequest == null)
            {
                // Find request where this user is the matched user, and get the linked request
                var requestWhereUserIsMatched = await _context.InterviewMatchingRequests
                    .Include(r => r.MatchedRequest)
                        .ThenInclude(r => r!.ScheduledSession)
                    .FirstOrDefaultAsync(r => r.MatchedUserId == userId);
                
                if (requestWhereUserIsMatched != null && requestWhereUserIsMatched.MatchedRequest != null)
                {
                    // Check if the matched request is for this session
                    if (requestWhereUserIsMatched.MatchedRequest.ScheduledSessionId == id)
                    {
                        // Load the full matched request
                        matchingRequest = await _context.InterviewMatchingRequests
                            .Include(r => r.MatchedRequest)
                                .ThenInclude(r => r!.User)
                            .Include(r => r.MatchedRequest)
                                .ThenInclude(r => r!.ScheduledSession)
                            .FirstOrDefaultAsync(r => r.Id == requestWhereUserIsMatched.MatchedRequest!.Id);
                    }
                }
            }

            // Also check if there's a request where this session's interviewer is the matched user
            // This handles the case where User 2 is checking status for their session
            // but the matching request is on User 1's session (User 1 matched with User 2)
            if (matchingRequest == null)
            {
                // Find request where this session's interviewer is the matched user
                matchingRequest = await _context.InterviewMatchingRequests
                    .Include(r => r.ScheduledSession)
                    .Include(r => r.MatchedRequest)
                        .ThenInclude(r => r!.User)
                    .Include(r => r.MatchedRequest)
                        .ThenInclude(r => r!.ScheduledSession)
                    .FirstOrDefaultAsync(r => 
                        r.MatchedUserId == session.InterviewerId &&
                        r.MatchedRequest != null &&
                        r.MatchedRequest.ScheduledSessionId == id);
            }

            if (matchingRequest == null)
            {
                return Ok(new { status = "NoRequest", id = (Guid?)null, userId = (Guid?)null, scheduledSessionId = id });
            }

            // Determine which side of the match this user is on
            var isUser1 = matchingRequest.UserId == userId;
            var isUser2 = matchingRequest.MatchedUserId == userId;
            
            // If user is on the matched side, we need to return the matched request's perspective
            if (isUser2 && matchingRequest.MatchedRequest != null)
            {
                // Return the matched request from User 2's perspective
                return Ok(new
                {
                    id = matchingRequest.MatchedRequest.Id,
                    status = matchingRequest.MatchedRequest.Status,
                    userId = matchingRequest.MatchedRequest.UserId,
                    matchedUserId = matchingRequest.MatchedRequest.MatchedUserId,
                    userConfirmed = matchingRequest.MatchedRequest.UserConfirmed,
                    matchedUserConfirmed = matchingRequest.MatchedRequest.MatchedUserConfirmed,
                    matchedRequest = new
                    {
                        id = matchingRequest.Id,
                        userId = matchingRequest.UserId
                    }
                });
            }

            // Return the matching request with all necessary fields (without user name)
            return Ok(new
            {
                id = matchingRequest.Id,
                status = matchingRequest.Status,
                userId = matchingRequest.UserId,
                matchedUserId = matchingRequest.MatchedUserId,
                userConfirmed = matchingRequest.UserConfirmed,
                matchedUserConfirmed = matchingRequest.MatchedUserConfirmed,
                matchedRequest = matchingRequest.MatchedRequest != null ? new
                {
                    id = matchingRequest.MatchedRequest.Id,
                    userId = matchingRequest.MatchedRequest.UserId
                    // Removed user name/email from response
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matching status for session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred while getting matching status" });
        }
    }

    [HttpPost("matching-requests/{id}/confirm")]
    public async Task<ActionResult> ConfirmMatch(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = await _peerInterviewService.ConfirmMatchAsync(id, userId);
            
            if (request == null)
            {
                return NotFound(new { message = "Matching request not found" });
            }

            // Reload request to get latest confirmation status
            var updatedRequest = await _context.InterviewMatchingRequests
                .Include(r => r.MatchedRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (updatedRequest == null)
            {
                return NotFound(new { message = "Matching request not found" });
            }

            // Check if both users confirmed, then complete the match
            if (updatedRequest.UserConfirmed && updatedRequest.MatchedUserConfirmed)
            {
                // Try to complete the match - use the request that has the session
                InterviewMatchingRequest? requestToComplete = updatedRequest;
                
                // Ensure we use the correct request ID for completion
                // The requestToComplete should be the one that will become the primary session
                var session = await _peerInterviewService.CompleteMatchAsync(requestToComplete.Id);
                if (session != null)
                {
                    // Reload session to get latest state
                    var finalSession = await _context.PeerInterviewSessions
                        .Include(s => s.Question)
                        .FirstOrDefaultAsync(s => s.Id == session.Id);
                    
                    if (finalSession == null)
                    {
                        return StatusCode(500, new { message = "Session not found after completion" });
                    }
                    
                    // Return minimal session data to avoid large response
                    return Ok(new { 
                        matchingRequest = new { 
                            id = updatedRequest.Id, 
                            status = updatedRequest.Status,
                            userConfirmed = updatedRequest.UserConfirmed,
                            matchedUserConfirmed = updatedRequest.MatchedUserConfirmed
                        }, 
                        session = new {
                            id = finalSession.Id.ToString(),
                            questionId = finalSession.QuestionId?.ToString(),
                            interviewerId = finalSession.InterviewerId.ToString(),
                            intervieweeId = finalSession.IntervieweeId?.ToString(),
                            status = finalSession.Status
                        }, 
                        completed = true 
                    });
                }
            }

            // NEW BEHAVIOR: Return session info immediately after user confirms
            // This allows users to be redirected right away, even if partner hasn't confirmed
            PeerInterviewSession? sessionToReturn = null;
            
            // If both confirmed, complete the match and return the merged session
            if (updatedRequest.UserConfirmed && updatedRequest.MatchedUserConfirmed)
            {
                var completedSession = await _peerInterviewService.CompleteMatchAsync(updatedRequest.Id);
                if (completedSession != null)
                {
                    sessionToReturn = completedSession;
                }
            }
            else
            {
                // Not both confirmed yet - return the primary session so user can start
                // The partner will join when they confirm, and both will use the same session
                sessionToReturn = await _peerInterviewService.GetSessionForMatchedRequestAsync(updatedRequest.Id, userId);
            }

            // Return session info so user can be redirected immediately
            if (sessionToReturn != null)
            {
                return Ok(new { 
                    matchingRequest = new { 
                        id = updatedRequest.Id, 
                        status = updatedRequest.Status,
                        userConfirmed = updatedRequest.UserConfirmed,
                        matchedUserConfirmed = updatedRequest.MatchedUserConfirmed
                    }, 
                    session = new {
                        id = sessionToReturn.Id.ToString(),
                        questionId = sessionToReturn.QuestionId?.ToString(),
                        interviewerId = sessionToReturn.InterviewerId.ToString(),
                        intervieweeId = sessionToReturn.IntervieweeId?.ToString(),
                        status = sessionToReturn.Status
                    }, 
                    completed = updatedRequest.UserConfirmed && updatedRequest.MatchedUserConfirmed
                });
            }

            // Return current status (match not completed yet)
            return Ok(new { 
                matchingRequest = new { 
                    id = updatedRequest.Id, 
                    status = updatedRequest.Status,
                    userConfirmed = updatedRequest.UserConfirmed,
                    matchedUserConfirmed = updatedRequest.MatchedUserConfirmed
                }, 
                completed = false 
            });
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
            _logger.LogError(ex, "Error confirming match");
            return StatusCode(500, new { message = "An error occurred while confirming match" });
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
    public Guid? IntervieweeId { get; set; } // Can be null for matching pending
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

