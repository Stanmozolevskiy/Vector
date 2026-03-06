using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/challenges")]
[Authorize]
public class ChallengeController : ControllerBase
{
    private readonly IChallengeService _challengeService;
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<ChallengeController> _logger;

    public ChallengeController(
        IChallengeService challengeService,
        IRecommendationService recommendationService,
        ILogger<ChallengeController> logger)
    {
        _challengeService = challengeService;
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Get today's daily challenge
    /// </summary>
    [HttpGet("daily")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDailyChallenge([FromQuery] DateTime? date = null)
    {
        try
        {
            var challenge = await _challengeService.GetDailyChallengeAsync(date);
            if (challenge == null)
            {
                return NotFound(new { error = "No daily challenge available" });
            }

            var userId = GetUserId();
            var attempt = await _challengeService.GetUserChallengeAttemptAsync(userId, challenge.Id);

            return Ok(new
            {
                challenge,
                userAttempt = attempt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily challenge");
            return StatusCode(500, new { error = "An error occurred while fetching the daily challenge" });
        }
    }

    /// <summary>
    /// Get past daily challenges
    /// </summary>
    [HttpGet("past")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPastChallenges([FromQuery] int limit = 7)
    {
        try
        {
            var challenges = await _challengeService.GetPastChallengesAsync(limit);
            return Ok(challenges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting past challenges");
            return StatusCode(500, new { error = "An error occurred while fetching past challenges" });
        }
    }

    /// <summary>
    /// Start a challenge attempt
    /// </summary>
    [HttpPost("{challengeId}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartChallenge(Guid challengeId)
    {
        try
        {
            var userId = GetUserId();
            var attempt = await _challengeService.StartChallengeAttemptAsync(userId, challengeId);
            
            return Ok(attempt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting challenge {ChallengeId}", challengeId);
            return StatusCode(500, new { error = "An error occurred while starting the challenge" });
        }
    }

    /// <summary>
    /// Complete a challenge attempt
    /// </summary>
    [HttpPost("{challengeId}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteChallenge(
        Guid challengeId,
        [FromBody] CompleteChallengeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { error = "Code is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest(new { error = "Language is required" });
            }

            var userId = GetUserId();
            var attempt = await _challengeService.CompleteChallengeAttemptAsync(
                userId,
                challengeId,
                request.Code,
                request.Language,
                request.TestCasesPassed,
                request.TotalTestCases);

            return Ok(attempt);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing challenge {ChallengeId}", challengeId);
            return StatusCode(500, new { error = "An error occurred while completing the challenge" });
        }
    }

    /// <summary>
    /// Get user's challenge history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengeHistory([FromQuery] int limit = 30)
    {
        try
        {
            var userId = GetUserId();
            var history = await _challengeService.GetChallengeHistoryAsync(userId, limit);
            
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting challenge history");
            return StatusCode(500, new { error = "An error occurred while fetching challenge history" });
        }
    }

    /// <summary>
    /// Get user's challenge statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengeStats()
    {
        try
        {
            var userId = GetUserId();
            var stats = await _challengeService.GetChallengeStatsAsync(userId);
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting challenge stats");
            return StatusCode(500, new { error = "An error occurred while fetching challenge statistics" });
        }
    }

    /// <summary>
    /// Get recommended questions for the user
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetUserId();
            var recommendations = await _recommendationService.GetRecommendedQuestionsAsync(userId, limit);
            
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return StatusCode(500, new { error = "An error occurred while fetching recommendations" });
        }
    }

    /// <summary>
    /// Get personalized problem set
    /// </summary>
    [HttpGet("personalized")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonalizedSet()
    {
        try
        {
            var userId = GetUserId();
            var personalizedSet = await _recommendationService.GetPersonalizedSetAsync(userId);
            
            return Ok(personalizedSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalized set");
            return StatusCode(500, new { error = "An error occurred while fetching personalized set" });
        }
    }

    /// <summary>
    /// Get questions to improve weak areas
    /// </summary>
    [HttpGet("weak-areas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeakAreaQuestions([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetUserId();
            var questions = await _recommendationService.GetWeakAreaQuestionsAsync(userId, limit);
            
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weak area questions");
            return StatusCode(500, new { error = "An error occurred while fetching weak area questions" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

public class CompleteChallengeRequest
{
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int TestCasesPassed { get; set; }
    public int TotalTestCases { get; set; }
}
