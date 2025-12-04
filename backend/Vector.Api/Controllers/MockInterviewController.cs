using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MockInterviewController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MockInterviewController> _logger;

    public MockInterviewController(ApplicationDbContext context, ILogger<MockInterviewController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active mock interview videos
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var interviews = await _context.MockInterviews
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.Description,
                    i.VideoUrl,
                    i.ThumbnailUrl,
                    i.DurationSeconds,
                    i.Category,
                    i.Difficulty,
                    i.CreatedAt
                })
                .ToListAsync();

            return Ok(new { interviews });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mock interviews");
            return StatusCode(500, new { message = "Failed to retrieve mock interviews" });
        }
    }

    /// <summary>
    /// Get a specific mock interview by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var interview = await _context.MockInterviews
                .Where(i => i.Id == id && i.IsActive)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.Description,
                    i.VideoUrl,
                    i.ThumbnailUrl,
                    i.DurationSeconds,
                    i.Category,
                    i.Difficulty,
                    i.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (interview == null)
            {
                return NotFound(new { message = "Mock interview not found" });
            }

            return Ok(interview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mock interview {Id}", id);
            return StatusCode(500, new { message = "Failed to retrieve mock interview" });
        }
    }
}

