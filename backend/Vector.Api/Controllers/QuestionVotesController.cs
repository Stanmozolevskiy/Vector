using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionVotesController : ControllerBase
{
    private readonly IQuestionVoteService _voteService;
    private readonly ILogger<QuestionVotesController> _logger;

    public QuestionVotesController(IQuestionVoteService voteService, ILogger<QuestionVotesController> logger)
    {
        _voteService = voteService;
        _logger = logger;
    }

    [HttpPost("questions/{questionId}/vote")]
    public async Task<IActionResult> VoteQuestion(Guid questionId, [FromBody] VoteRequest request)
    {
        var userId = GetCurrentUserId();
        var voteCount = await _voteService.VoteQuestionAsync(questionId, userId, request.VoteType);
        return Ok(new { voteCount, message = request.VoteType == 1 ? "Upvoted" : "Downvoted" });
    }

    [HttpDelete("questions/{questionId}/vote")]
    public async Task<IActionResult> RemoveVote(Guid questionId)
    {
        var userId = GetCurrentUserId();
        var success = await _voteService.RemoveVoteAsync(questionId, userId);
        return success ? Ok(new { message = "Vote removed" }) : BadRequest(new { message = "No vote to remove" });
    }

    [HttpGet("questions/{questionId}/count")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVoteCount(Guid questionId)
    {
        var count = await _voteService.GetQuestionVoteCountAsync(questionId);
        return Ok(new { voteCount = count });
    }

    [HttpGet("questions/{questionId}/my-vote")]
    public async Task<IActionResult> GetMyVote(Guid questionId)
    {
        var userId = GetCurrentUserId();
        var vote = await _voteService.GetUserVoteAsync(questionId, userId);
        return Ok(new { voteType = vote });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

public class VoteRequest
{
    public int VoteType { get; set; } // 1 = upvote, -1 = downvote
}
