using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SiteSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SiteSettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get dashboard video settings (public - no auth required)
    /// </summary>
    [HttpGet("dashboard-video")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DashboardVideoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardVideo()
    {
        var url = await GetSettingAsync("DashboardVideoUrl");
        var title = await GetSettingAsync("DashboardVideoTitle");
        var description = await GetSettingAsync("DashboardVideoDescription");

        // Default fallback
        if (string.IsNullOrEmpty(url))
        {
            url = "https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/videos/mock-interviews/what-is-exponent.mp4";
        }
        if (string.IsNullOrEmpty(title))
        {
            title = "What Is Exponent? - Introduction to Mock Interviews";
        }
        if (string.IsNullOrEmpty(description))
        {
            description = "Learn how to prepare for technical interviews effectively with this introduction to mock interviews.";
        }

        return Ok(new DashboardVideoDto
        {
            Url = url,
            Title = title,
            Description = description
        });
    }

    private async Task<string?> GetSettingAsync(string key)
    {
        var setting = await _context.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }
}

public class DashboardVideoDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
