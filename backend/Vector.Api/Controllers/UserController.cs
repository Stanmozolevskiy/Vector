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
    private readonly IRedisService _redisService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, IJwtService jwtService, IRedisService redisService, ILogger<UserController> logger)
    {
        _userService = userService;
        _jwtService = jwtService;
        _redisService = redisService;
        _logger = logger;
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

        // Try to get user from Redis cache first (FAST)
        var cachedUser = await _redisService.GetCachedUserSessionAsync<Models.User>(userId);
        
        Models.User user;
        if (cachedUser != null)
        {
            _logger.LogInformation("User {UserId} fetched from Redis cache", userId);
            user = cachedUser;
        }
        else
        {
            // Cache miss - fetch from database
            user = await _userService.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Cache user session in Redis for 5 minutes
            await _redisService.CacheUserSessionAsync(userId, user, TimeSpan.FromMinutes(5));
            _logger.LogInformation("User {UserId} fetched from database and cached in Redis", userId);
        }

        // Return user data (exclude sensitive information)
            return Ok(new
            {
                id = user.Id.ToString(),
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                bio = user.Bio,
                phoneNumber = user.PhoneNumber,
                location = user.Location,
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
            _logger.LogInformation("Updating profile for user {UserId} with data: {@ProfileData}", userId, dto);
            
            var user = await _userService.UpdateProfileAsync(userId, dto);
            
            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
            
            return Ok(new
            {
                id = user.Id.ToString(),
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                bio = user.Bio,
                phoneNumber = user.PhoneNumber,
                location = user.Location,
                role = user.Role,
                profilePictureUrl = user.ProfilePictureUrl,
                emailVerified = user.EmailVerified,
                updatedAt = user.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User not found: {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
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

    /// <summary>
    /// Upload profile picture
    /// </summary>
    /// <param name="file">Image file to upload</param>
    /// <returns>Profile picture URL</returns>
    [HttpPost("me/profile-picture")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { error = "Invalid file type. Only JPEG, PNG, and GIF are allowed" });
        }

        // Validate file size (5MB max)
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { error = "File size exceeds 5MB limit" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var pictureUrl = await _userService.UploadProfilePictureAsync(userId, stream, file.FileName, file.ContentType);
            
            _logger.LogInformation("Profile picture uploaded successfully for user {UserId}: {Url}", userId, pictureUrl);
            
            return Ok(new { profilePictureUrl = pictureUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to upload profile picture" });
        }
    }

    /// <summary>
    /// Delete profile picture
    /// </summary>
    /// <returns>Success message</returns>
    [HttpDelete("me/profile-picture")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        try
        {
            var success = await _userService.DeleteProfilePictureAsync(userId);
            
            if (!success)
            {
                return NotFound(new { error = "No profile picture to delete" });
            }
            
            _logger.LogInformation("Profile picture deleted successfully for user {UserId}", userId);
            
            return Ok(new { message = "Profile picture deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile picture for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to delete profile picture" });
        }
    }

    /// <summary>
    /// Delete current user's account
    /// </summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        try
        {
            var deleted = await _userService.DeleteUserAsync(userId);
            if (!deleted)
            {
                return NotFound(new { error = "User not found" });
            }

            _logger.LogInformation("Account deleted for user {UserId}", userId);

            return Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete account for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to delete account" });
        }
    }

    // TODO: Implement remaining endpoints
    // - GET /api/users/:id (public profile view)
}

