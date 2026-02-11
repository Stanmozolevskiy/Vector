using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    [HttpPost("questions/{questionId}")]
    public async Task<IActionResult> CreateComment(Guid questionId, [FromBody] CreateCommentRequest request)
    {
        var userId = GetCurrentUserId();
        var comment = await _commentService.CreateCommentAsync(questionId, userId, request.Content);
        return Ok(comment);
    }

    [HttpGet("questions/{questionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid questionId)
    {
        var comments = await _commentService.GetCommentsForQuestionAsync(questionId);
        return Ok(comments);
    }

    [HttpPut("{commentId}")]
    public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] UpdateCommentRequest request)
    {
        var userId = GetCurrentUserId();
        var comment = await _commentService.UpdateCommentAsync(commentId, userId, request.Content);
        return Ok(comment);
    }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var success = await _commentService.DeleteCommentAsync(commentId, userId);
        return success ? Ok() : NotFound();
    }

    [HttpPost("{commentId}/upvote")]
    public async Task<IActionResult> UpvoteComment(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var success = await _commentService.UpvoteCommentAsync(commentId, userId);
        return success ? Ok(new { message = "Comment upvoted successfully" }) : BadRequest(new { message = "Already upvoted" });
    }

    [HttpDelete("{commentId}/upvote")]
    public async Task<IActionResult> RemoveUpvote(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var success = await _commentService.RemoveUpvoteAsync(commentId, userId);
        return success ? Ok(new { message = "Upvote removed" }) : BadRequest(new { message = "Not upvoted" });
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

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
