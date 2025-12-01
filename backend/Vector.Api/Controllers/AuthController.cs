using Microsoft.AspNetCore.Mvc;
using Vector.Api.DTOs.Auth;
using Vector.Api.Models;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
        catch (Exception)
        {
            // Log error in production
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
                return Ok(new { message = "Email verified successfully." });
            }
            else
            {
                return BadRequest(new { error = "Invalid or expired verification token." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email with token: {Token}", token);
            return StatusCode(500, new { error = "An error occurred while verifying the email." });
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

        try
        {
            var accessToken = await _authService.LoginAsync(dto);
            
            return Ok(new 
            { 
                accessToken,
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
            return StatusCode(500, new { error = "An error occurred while logging in." });
        }
    }

    // TODO: Implement remaining endpoints
    // - POST /api/auth/logout
    // - POST /api/auth/refresh-token
    // - POST /api/auth/forgot-password
    // - POST /api/auth/reset-password
}

