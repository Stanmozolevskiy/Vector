using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.DTOs.Common;
using Vector.Api.DTOs.User;
using Vector.Api.Helpers;
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
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
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
                return NotFound(new ApiErrorResponse("User not found", "USER_NOT_FOUND"));
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
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        try
        {
            _logger.LogInformation("Updating profile for user {UserId} with data: {@ProfileData}", userId, dto);
            
            var user = await _userService.UpdateProfileAsync(userId, dto);
            
            if (user == null)
            {
                return NotFound(new ApiErrorResponse("User not found", "USER_NOT_FOUND"));
            }
            
            // Invalidate Redis cache to ensure fresh data on next request
            await _redisService.InvalidateUserSessionAsync(userId);
            _logger.LogInformation("Invalidated Redis cache for user {UserId}", userId);
            
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
            return NotFound(new ApiErrorResponse(ex.Message, "USER_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return StatusCode(500, new ApiErrorResponse("An error occurred while updating your profile. Please try again later.", "UPDATE_ERROR"));
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
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(ApiErrorResponse.ValidationError(errors));
        }

        try
        {
            await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            
            return Ok(new { message = "Password changed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new ApiErrorResponse(ex.Message, "INVALID_PASSWORD"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiErrorResponse(ex.Message, "USER_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return StatusCode(500, new ApiErrorResponse("An error occurred while changing your password. Please try again later.", "PASSWORD_CHANGE_ERROR"));
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
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        // Validate image file using helper
        var (isValid, errorMessage, errorCode) = ImageHelper.ValidateImage(file);
        if (!isValid)
        {
            return BadRequest(new ApiErrorResponse(errorMessage ?? "Invalid image file", errorCode));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var pictureUrl = await _userService.UploadProfilePictureAsync(userId, stream, file.FileName, file.ContentType);
            
            _logger.LogInformation("Profile picture uploaded successfully for user {UserId}: {Url}", userId, pictureUrl);
            
            // Invalidate Redis cache to ensure fresh data on next request
            await _redisService.InvalidateUserSessionAsync(userId);
            _logger.LogInformation("Invalidated Redis cache for user {UserId} after profile picture upload", userId);
            
            return Ok(new { profilePictureUrl = pictureUrl });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while uploading profile picture for user {UserId}", userId);
            return BadRequest(new ApiErrorResponse(ex.Message, "UPLOAD_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
            return StatusCode(500, new ApiErrorResponse("An error occurred while uploading the profile picture. Please try again later.", "UPLOAD_ERROR"));
        }
    }

    /// <summary>
    /// Delete current user's account
    /// </summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        try
        {
            var deleted = await _userService.DeleteUserAsync(userId);
            if (!deleted)
            {
                return NotFound(new ApiErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            _logger.LogInformation("Account deleted for user {UserId}", userId);

            return Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete account for user {UserId}", userId);
            return StatusCode(500, new ApiErrorResponse("An error occurred while deleting your account. Please try again later.", "DELETE_ACCOUNT_ERROR"));
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
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        try
        {
            var success = await _userService.DeleteProfilePictureAsync(userId);
            
            if (!success)
            {
                return NotFound(new ApiErrorResponse("No profile picture found to delete", "NO_PICTURE"));
            }
            
            _logger.LogInformation("Profile picture deleted successfully for user {UserId}", userId);
            
            // Invalidate Redis cache to ensure fresh data on next request
            await _redisService.InvalidateUserSessionAsync(userId);
            _logger.LogInformation("Invalidated Redis cache for user {UserId} after profile picture deletion", userId);
            
            return Ok(new { message = "Profile picture deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile picture for user {UserId}", userId);
            return StatusCode(500, new ApiErrorResponse("An error occurred while deleting the profile picture. Please try again later.", "DELETE_ERROR"));
        }
    }

    // TODO: Implement remaining endpoints
    // - GET /api/users/:id (public profile view)
}

