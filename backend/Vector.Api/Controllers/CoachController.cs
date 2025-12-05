using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Coach;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoachController : ControllerBase
{
    private readonly ICoachService _coachService;
    private readonly ILogger<CoachController> _logger;

    public CoachController(ICoachService coachService, ILogger<CoachController> logger)
    {
        _coachService = coachService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a coach application
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(CoachApplicationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitCoachApplicationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var application = await _coachService.SubmitApplicationAsync(userId.Value, dto);

            var response = new CoachApplicationResponseDto
            {
                Id = application.Id,
                UserId = application.UserId,
                Motivation = application.Motivation,
                Experience = application.Experience,
                Specialization = application.Specialization,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            };

            return StatusCode(201, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting coach application");
            return StatusCode(500, new { error = "An error occurred while submitting your application." });
        }
    }

    /// <summary>
    /// Get current user's coach application status
    /// </summary>
    [HttpGet("my-application")]
    [ProducesResponseType(typeof(CoachApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyApplication()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var application = await _coachService.GetApplicationByUserIdAsync(userId.Value);

            if (application == null)
            {
                return NotFound(new { error = "No application found" });
            }

            var response = MapToResponseDto(application);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving coach application");
            return StatusCode(500, new { error = "An error occurred while retrieving your application." });
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

    private CoachApplicationResponseDto MapToResponseDto(Models.CoachApplication application)
    {
        return new CoachApplicationResponseDto
        {
            Id = application.Id,
            UserId = application.UserId,
            UserEmail = application.User?.Email ?? string.Empty,
            UserName = $"{application.User?.FirstName} {application.User?.LastName}".Trim(),
            Motivation = application.Motivation,
            Experience = application.Experience,
            Specialization = application.Specialization,
            Status = application.Status,
            AdminNotes = application.AdminNotes,
            ReviewedBy = application.ReviewedBy,
            ReviewerName = application.Reviewer != null 
                ? $"{application.Reviewer.FirstName} {application.Reviewer.LastName}".Trim()
                : null,
            ReviewedAt = application.ReviewedAt,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt
        };
    }
}

