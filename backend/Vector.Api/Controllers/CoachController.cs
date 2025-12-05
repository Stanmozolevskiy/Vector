using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Coach;
using Vector.Api.Services;
using Vector.Api.Models;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoachController : ControllerBase
{
    private readonly ICoachService _coachService;
    private readonly IS3Service _s3Service;
    private readonly ILogger<CoachController> _logger;

    public CoachController(ICoachService coachService, IS3Service s3Service, ILogger<CoachController> logger)
    {
        _coachService = coachService;
        _s3Service = s3Service;
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

            var response = MapToResponseDto(await _coachService.GetApplicationByUserIdAsync(userId.Value));

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

    /// <summary>
    /// Upload images for coach application (portfolio, certificates, etc.)
    /// </summary>
    [HttpPost("upload-image")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { error = "Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed" });
        }

        // Validate file size (10MB max for portfolio images)
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "File size exceeds 10MB limit" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var imageUrl = await _s3Service.UploadFileAsync(
                stream, 
                file.FileName, 
                file.ContentType, 
                "coach-applications"
            );

            _logger.LogInformation("Coach application image uploaded successfully for user {UserId}: {Url}", userId, imageUrl);

            return Ok(new { imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload coach application image for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to upload image" });
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
            ImageUrls = !string.IsNullOrEmpty(application.ImageUrls)
                ? application.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : null,
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

