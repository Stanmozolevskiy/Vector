using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            role = user.Role,
            profilePictureUrl = user.ProfilePictureUrl,
            emailVerified = user.EmailVerified,
            createdAt = user.CreatedAt
        });
    }

    // TODO: Implement remaining endpoints
    // - PUT /api/users/me
    // - DELETE /api/users/me
    // - GET /api/users/:id (public profile)
    // - PUT /api/users/me/password
    // - POST /api/users/me/profile-picture
    // - DELETE /api/users/me/profile-picture
}

