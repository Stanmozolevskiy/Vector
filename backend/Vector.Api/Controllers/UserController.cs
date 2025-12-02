using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.DTOs.User;
using Vector.Api.Services;
using System.Security.Claims;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public UserController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Get current authenticated user's information.
    /// </summary>
    /// <returns>Current user information</returns>
    /// <response code="200">Returns the current user</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Get user ID from JWT token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value 
                        ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Return user data (exclude sensitive information)
        return Ok(new
        {
            id = user.Id.ToString(),
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            bio = user.Bio,
            role = user.Role,
            profilePictureUrl = user.ProfilePictureUrl,
            emailVerified = user.EmailVerified,
            createdAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Update current user's profile information
    /// </summary>
    /// <param name="dto">Profile update data</param>
    /// <returns>Updated user information</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        try
        {
            var user = await _userService.UpdateProfileAsync(userId, dto);
            
            return Ok(new
            {
                id = user.Id.ToString(),
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                bio = user.Bio,
                role = user.Role,
                profilePictureUrl = user.ProfilePictureUrl,
                emailVerified = user.EmailVerified,
                updatedAt = user.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating profile" });
        }
    }

    /// <summary>
    /// Change user's password
    /// </summary>
    /// <param name="dto">Password change data</param>
    /// <returns>Success message</returns>
    [HttpPut("me/password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            
            return Ok(new { message = "Password changed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An error occurred while changing password" });
        }
    }

    // TODO: Implement remaining endpoints
    // - DELETE /api/users/me
    // - GET /api/users/:id (public profile)
    // - POST /api/users/me/profile-picture (requires S3 integration)
    // - DELETE /api/users/me/profile-picture (requires S3 integration)
}

