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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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

    // TODO: Implement remaining endpoints
    // - POST /api/auth/login
    // - POST /api/auth/logout
    // - POST /api/auth/refresh-token
    // - GET /api/auth/verify-email
    // - POST /api/auth/forgot-password
    // - POST /api/auth/reset-password
}

