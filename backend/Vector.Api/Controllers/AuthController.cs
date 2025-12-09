using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Auth;
using Vector.Api.Models;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRedisService _redisService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IRedisService redisService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="dto">Registration data</param>
    /// <returns>Created user (without password)</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _authService.RegisterUserAsync(dto);
            
            // Return user without sensitive data
            var response = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.EmailVerified,
                user.CreatedAt
            };
            
            return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred while registering the user." });
        }
    }

    /// <summary>
    /// Verify user email address using verification token
    /// </summary>
    /// <param name="token">Email verification token from the verification email</param>
    /// <returns>Success message if email is verified</returns>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid or expired verification token</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("verify-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { error = "Verification token is required." });
        }

        try
        {
            var result = await _authService.VerifyEmailAsync(token);
            
            if (result)
            {
                return Ok(new { message = "Email verified successfully. You can now log in." });
            }
            else
            {
                return BadRequest(new { error = "Invalid or expired verification token." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for token: {Token}", token);
            return StatusCode(500, new { error = "An error occurred while verifying the email." });
        }
    }

    /// <summary>
    /// Resend email verification link
    /// </summary>
    /// <param name="dto">Email address to resend verification to</param>
    /// <returns>Success message</returns>
    /// <response code="200">Verification email sent (or email not found for security)</response>
    /// <response code="400">Invalid email format</response>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _authService.ResendVerificationEmailAsync(dto.Email);
            // Always return success for security (don't reveal if email exists)
            return Ok(new { message = "If your account exists and is not verified, a new verification email has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resend verification for email: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred while resending verification email." });
        }
    }

    /// <summary>
    /// Login user and return JWT access token
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <returns>JWT access token</returns>
    /// <response code="200">Login successful, returns access token</response>
    /// <response code="400">Invalid credentials or email not verified</response>
    /// <response code="401">Unauthorized - invalid email or password</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Rate limiting: Max 5 login attempts per 15 minutes per email
        var rateLimitKey = $"login:{dto.Email.ToLower()}";
        var isAllowed = await _redisService.CheckRateLimitAsync(rateLimitKey, 5, TimeSpan.FromMinutes(15));
        
        if (!isAllowed)
        {
            var attempts = await _redisService.GetRateLimitAttemptsAsync(rateLimitKey);
            _logger.LogWarning("Rate limit exceeded for login attempt: {Email}, Attempts: {Attempts}", dto.Email, attempts);
            return StatusCode(429, new 
            { 
                error = "Too many login attempts. Please try again in 15 minutes." 
            });
        }

        try
        {
            var (accessToken, refreshToken) = await _authService.LoginAsync(dto);
            
            // Reset rate limit on successful login
            await _redisService.ResetRateLimitAsync(rateLimitKey);
            
            return Ok(new 
            { 
                accessToken,
                refreshToken,
                tokenType = "Bearer"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred during login." });
        }
    }

    /// <summary>
    /// Request a password reset link to be sent to the user's email.
    /// </summary>
    /// <param name="dto">Email address for password reset</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset link sent successfully (or email not found for security reasons)</response>
    /// <response code="400">Invalid email format</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            // Always return 200 OK for security reasons, even if email not found
            return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password request for email: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred while processing your request." });
        }
    }

    /// <summary>
    /// Reset user password using a valid reset token.
    /// </summary>
    /// <param name="dto">Reset password data (token, email, new password)</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Invalid or expired token, or invalid email</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (result)
            {
                return Ok(new { message = "Password has been reset successfully." });
            }
            else
            {
                return BadRequest(new { error = "Invalid or expired password reset token, or email mismatch." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email: {Email}", dto.Email);
            return StatusCode(500, new { error = "An error occurred while resetting the password." });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="dto">Refresh token data</param>
    /// <returns>New access token and refresh token</returns>
    /// <response code="200">Tokens refreshed successfully</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var (accessToken, refreshToken) = await _authService.RefreshTokenAsync(dto.RefreshToken);
            
            return Ok(new 
            { 
                accessToken,
                refreshToken,
                tokenType = "Bearer"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Refresh token failed: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { error = "An error occurred while refreshing the token." });
        }
    }

    /// <summary>
    /// Logout user and invalidate refresh token
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        try
        {
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            // Still return success even if logout fails - token will expire anyway
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
