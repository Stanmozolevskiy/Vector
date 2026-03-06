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
        _logger.LogInformation("Creating comment for question {QuestionId}. Content: {Content}, Type: {CommentType}", 
            questionId, request.Content?.Substring(0, Math.Min(50, request.Content?.Length ?? 0)), request.CommentType);
        
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User ID: {UserId}", userId);
            
            var comment = await _commentService.CreateCommentAsync(
                questionId, 
                userId, 
                request.Content, 
                request.CommentType, 
                request.ParentCommentId
            );
            
            _logger.LogInformation("Comment created successfully with ID: {CommentId}", comment.Id);
            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for question {QuestionId}", questionId);
            return StatusCode(500, new { message = "Failed to create comment", error = ex.Message });
        }
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
        // Return Ok even if already upvoted (idempotent operation)
        return Ok(new { message = success ? "Comment upvoted successfully" : "Already upvoted" });
    }

    [HttpDelete("{commentId}/upvote")]
    public async Task<IActionResult> RemoveUpvote(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var success = await _commentService.RemoveUpvoteAsync(commentId, userId);
        return success ? Ok(new { message = "Upvote removed" }) : Ok(new { message = "Not upvoted" });
    }

    [HttpPost("{commentId}/downvote")]
    public async Task<IActionResult> DownvoteComment(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var success = await _commentService.DownvoteCommentAsync(commentId, userId);
        return success ? Ok(new { message = "Comment downvoted successfully" }) : Ok(new { message = "Cannot downvote - you must upvote first" });
    }

    [HttpDelete("{commentId}/downvote")]
    public async Task<IActionResult> RemoveDownvote(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var success = await _commentService.RemoveDownvoteAsync(commentId, userId);
        return success ? Ok(new { message = "Downvote removed" }) : Ok(new { message = "Not downvoted" });
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
    public string? CommentType { get; set; }
    public Guid? ParentCommentId { get; set; }
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
