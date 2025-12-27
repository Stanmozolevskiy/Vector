using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.Models;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/video-sessions")]
[Authorize]
public class VideoSessionController : ControllerBase
{
    private readonly IVideoSessionService _videoSessionService;
    private readonly ILogger<VideoSessionController> _logger;

    public VideoSessionController(
        IVideoSessionService videoSessionService,
        ILogger<VideoSessionController> logger)
    {
        _videoSessionService = videoSessionService;
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

    [HttpPost("create")]
    public async Task<ActionResult<VideoSession>> CreateVideoSession([FromBody] CreateVideoSessionRequest request)
    {
        try
        {
            var videoSession = await _videoSessionService.CreateVideoSessionAsync(request.SessionId);
            return Ok(videoSession);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating video session");
            return StatusCode(500, new { message = "An error occurred while creating the video session" });
        }
    }

    [HttpGet("{id}/token")]
    public async Task<ActionResult<VideoSession>> GetVideoSessionToken(Guid id)
    {
        try
        {
            var videoSession = await _videoSessionService.GetVideoSessionBySessionIdAsync(id);
            if (videoSession == null)
            {
                return NotFound(new { message = "Video session not found" });
            }

            return Ok(new { token = videoSession.Token, videoSessionId = videoSession.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video session token");
            return StatusCode(500, new { message = "An error occurred while getting the video session token" });
        }
    }

    [HttpGet("token/{token}")]
    public async Task<ActionResult<VideoSession>> GetVideoSessionByToken(string token)
    {
        try
        {
            var videoSession = await _videoSessionService.GetVideoSessionByTokenAsync(token);
            if (videoSession == null)
            {
                return NotFound(new { message = "Video session not found" });
            }

            return Ok(videoSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video session by token");
            return StatusCode(500, new { message = "An error occurred while getting the video session" });
        }
    }

    [HttpPut("{id}/signaling")]
    public async Task<ActionResult> UpdateSignalingData(Guid id, [FromBody] UpdateSignalingDataRequest request)
    {
        try
        {
            await _videoSessionService.UpdateSignalingDataAsync(id, request.SignalingData);
            return Ok(new { message = "Signaling data updated" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating signaling data");
            return StatusCode(500, new { message = "An error occurred while updating signaling data" });
        }
    }

    [HttpPost("{id}/end")]
    public async Task<ActionResult> EndVideoSession(Guid id)
    {
        try
        {
            var success = await _videoSessionService.EndVideoSessionAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Video session not found" });
            }

            return Ok(new { message = "Video session ended" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending video session");
            return StatusCode(500, new { message = "An error occurred while ending the video session" });
        }
    }

    [HttpPost("{id}/offer")]
    public async Task<ActionResult> HandleOffer(Guid id, [FromBody] WebRTCOfferRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var videoSession = await _videoSessionService.GetVideoSessionBySessionIdAsync(id);
            
            if (videoSession == null)
            {
                return NotFound(new { message = "Video session not found" });
            }

            // Verify user has access to this session
            var session = videoSession.Session;
            if (session.InterviewerId != userId && session.IntervieweeId != userId)
            {
                return Forbid("You do not have access to this session");
            }

            // Store the offer in signaling data
            var signalingData = new
            {
                offer = request.Offer,
                fromUserId = userId.ToString(),
                timestamp = DateTime.UtcNow
            };

            await _videoSessionService.UpdateSignalingDataAsync(videoSession.Id, System.Text.Json.JsonSerializer.Serialize(signalingData));

            return Ok(new { message = "Offer received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebRTC offer");
            return StatusCode(500, new { message = "An error occurred while handling the offer" });
        }
    }

    [HttpPost("{id}/answer")]
    public async Task<ActionResult> HandleAnswer(Guid id, [FromBody] WebRTCAnswerRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var videoSession = await _videoSessionService.GetVideoSessionBySessionIdAsync(id);
            
            if (videoSession == null)
            {
                return NotFound(new { message = "Video session not found" });
            }

            // Verify user has access to this session
            var session = videoSession.Session;
            if (session.InterviewerId != userId && session.IntervieweeId != userId)
            {
                return Forbid("You do not have access to this session");
            }

            // Store the answer in signaling data
            var signalingData = new
            {
                answer = request.Answer,
                fromUserId = userId.ToString(),
                timestamp = DateTime.UtcNow
            };

            await _videoSessionService.UpdateSignalingDataAsync(videoSession.Id, System.Text.Json.JsonSerializer.Serialize(signalingData));

            return Ok(new { message = "Answer received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebRTC answer");
            return StatusCode(500, new { message = "An error occurred while handling the answer" });
        }
    }

    [HttpPost("{id}/ice-candidate")]
    public async Task<ActionResult> HandleIceCandidate(Guid id, [FromBody] WebRTCIceCandidateRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var videoSession = await _videoSessionService.GetVideoSessionBySessionIdAsync(id);
            
            if (videoSession == null)
            {
                return NotFound(new { message = "Video session not found" });
            }

            // Verify user has access to this session
            var session = videoSession.Session;
            if (session.InterviewerId != userId && session.IntervieweeId != userId)
            {
                return Forbid("You do not have access to this session");
            }

            // Store ICE candidate in signaling data
            var existingData = videoSession.SignalingData;
            var signalingData = string.IsNullOrEmpty(existingData)
                ? new { iceCandidates = new[] { new { candidate = request.Candidate, sdpMLineIndex = request.SdpMLineIndex, sdpMid = request.SdpMid, fromUserId = userId.ToString(), timestamp = DateTime.UtcNow } } }
                : System.Text.Json.JsonSerializer.Deserialize<dynamic>(existingData);

            // Add new ICE candidate to existing data
            // For simplicity, we'll store as JSON string
            var updatedData = new
            {
                iceCandidate = new
                {
                    candidate = request.Candidate,
                    sdpMLineIndex = request.SdpMLineIndex,
                    sdpMid = request.SdpMid,
                    fromUserId = userId.ToString(),
                    timestamp = DateTime.UtcNow
                }
            };

            await _videoSessionService.UpdateSignalingDataAsync(videoSession.Id, System.Text.Json.JsonSerializer.Serialize(updatedData));

            return Ok(new { message = "ICE candidate received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ICE candidate");
            return StatusCode(500, new { message = "An error occurred while handling the ICE candidate" });
        }
    }

    [HttpGet("{id}/signaling")]
    public async Task<ActionResult> GetSignalingData(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var videoSession = await _videoSessionService.GetVideoSessionBySessionIdAsync(id);
            
            if (videoSession == null)
            {
                return NotFound(new { message = "Video session not found" });
            }

            // Verify user has access to this session
            var session = videoSession.Session;
            if (session.InterviewerId != userId && session.IntervieweeId != userId)
            {
                return Forbid("You do not have access to this session");
            }

            return Ok(new { signalingData = videoSession.SignalingData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signaling data");
            return StatusCode(500, new { message = "An error occurred while getting signaling data" });
        }
    }
}

public class WebRTCOfferRequest
{
    public string Offer { get; set; } = string.Empty;
}

public class WebRTCAnswerRequest
{
    public string Answer { get; set; } = string.Empty;
}

public class WebRTCIceCandidateRequest
{
    public string Candidate { get; set; } = string.Empty;
    public int? SdpMLineIndex { get; set; }
    public string? SdpMid { get; set; }
}

public class CreateVideoSessionRequest
{
    public Guid SessionId { get; set; }
}

public class UpdateSignalingDataRequest
{
    public string SignalingData { get; set; } = string.Empty;
}

