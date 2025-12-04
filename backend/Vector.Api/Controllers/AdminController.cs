using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vector.Api.Attributes;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Must be authenticated
[AuthorizeRole("admin")] // Must have admin role
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all users (admin only)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Role,
                    u.EmailVerified,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(new { users, total = users.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all users");
            return StatusCode(500, new { message = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Get user statistics (admin only)
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var verifiedUsers = await _context.Users.CountAsync(u => u.EmailVerified);
            var studentCount = await _context.Users.CountAsync(u => u.Role == "student");
            var coachCount = await _context.Users.CountAsync(u => u.Role == "coach");
            var adminCount = await _context.Users.CountAsync(u => u.Role == "admin");

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalUsers,
                verifiedUsers,
                unverifiedUsers = totalUsers - verifiedUsers,
                roleBreakdown = new
                {
                    students = studentCount,
                    coaches = coachCount,
                    admins = adminCount
                },
                recentUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return StatusCode(500, new { message = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Update user role (admin only)
    /// </summary>
    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate role
            var validRoles = new[] { "student", "coach", "admin" };
            if (!validRoles.Contains(dto.Role.ToLower()))
            {
                return BadRequest(new { message = $"Invalid role. Valid roles: {string.Join(", ", validRoles)}" });
            }

            user.Role = dto.Role.ToLower();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} role updated to {Role}", userId, dto.Role);

            return Ok(new
            {
                message = "User role updated successfully",
                userId = user.Id,
                newRole = user.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user role for {UserId}", userId);
            return StatusCode(500, new { message = "Failed to update user role" });
        }
    }

    /// <summary>
    /// Delete user (admin only)
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent deleting the last admin
            if (user.Role == "admin")
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "admin");
                if (adminCount <= 1)
                {
                    return BadRequest(new { message = "Cannot delete the last admin user" });
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted by admin", userId);

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to delete user" });
        }
    }
}

public class UpdateRoleDto
{
    public string Role { get; set; } = string.Empty;
}

