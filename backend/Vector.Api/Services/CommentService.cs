using Microsoft.EntityFrameworkCore;
using Vector.Api.Constants;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly ICoinService _coinService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ApplicationDbContext context,
        ICoinService coinService,
        ILogger<CommentService> logger)
    {
        _context = context;
        _coinService = coinService;
        _logger = logger;
    }

    public async Task<InterviewQuestionComment> CreateCommentAsync(Guid questionId, Guid userId, string content, string? commentType = null, Guid? parentCommentId = null)
    {
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new KeyNotFoundException("Question not found");
        }

        // Validate parent comment if provided
        if (parentCommentId.HasValue)
        {
            var parentComment = await _context.InterviewQuestionComments.FindAsync(parentCommentId.Value);
            if (parentComment == null || parentComment.QuestionId != questionId)
            {
                throw new KeyNotFoundException("Parent comment not found or does not belong to this question");
            }
        }

        var comment = new InterviewQuestionComment
        {
            QuestionId = questionId,
            UserId = userId,
            Content = content,
            CommentType = commentType,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InterviewQuestionComments.Add(comment);
        await _context.SaveChangesAsync();

        // Load the user relationship before returning
        await _context.Entry(comment).Reference(c => c.User).LoadAsync();

        return comment;
    }

    public async Task<IEnumerable<InterviewQuestionComment>> GetCommentsForQuestionAsync(Guid questionId)
    {
        // Return ALL comments (including replies) so the frontend can organize them
        return await _context.InterviewQuestionComments
            .Include(c => c.User)
            .Include(c => c.Votes)
            .Where(c => c.QuestionId == questionId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<InterviewQuestionComment> UpdateCommentAsync(Guid commentId, Guid userId, string content)
    {
        var comment = await _context.InterviewQuestionComments.FindAsync(commentId);
        if (comment == null)
        {
            throw new KeyNotFoundException("Comment not found");
        }

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only edit your own comments");
        }

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return comment;
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await _context.InterviewQuestionComments.FindAsync(commentId);
        if (comment == null) return false;

        if (comment.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own comments");
        }

        _context.InterviewQuestionComments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpvoteCommentAsync(Guid commentId, Guid userId)
    {
        // Check if already upvoted
        var existingVote = await _context.InterviewQuestionCommentVotes
            .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

        if (existingVote != null)
        {
            return false; // Already upvoted (idempotent - no error)
        }

        var comment = await _context.InterviewQuestionComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId);
            
        if (comment == null)
        {
            throw new KeyNotFoundException("Comment not found");
        }

        // Add upvote
        var vote = new InterviewQuestionCommentVote
        {
            CommentId = commentId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.InterviewQuestionCommentVotes.Add(vote);
        await _context.SaveChangesAsync();

        // Award coins to comment author ONLY if they didn't upvote their own comment
        if (comment.UserId != userId)
        {
            try
            {
                await _coinService.AwardCoinsAsync(
                    comment.UserId,
                    AchievementTypes.CommentUpvoted,
                    "Your comment was upvoted",
                    commentId,
                    "InterviewQuestionComment");

                _logger.LogInformation("Awarded CommentUpvoted coins to user {UserId} for comment {CommentId}",
                    comment.UserId, commentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to award CommentUpvoted coins to user {UserId}", comment.UserId);
            }
        }
        else
        {
            _logger.LogInformation("User {UserId} upvoted their own comment {CommentId}, no coins awarded", userId, commentId);
        }

        return true;
    }

    public async Task<bool> RemoveUpvoteAsync(Guid commentId, Guid userId)
    {
        var vote = await _context.InterviewQuestionCommentVotes
            .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

        if (vote == null)
        {
            return false; // Not upvoted
        }

        _context.InterviewQuestionCommentVotes.Remove(vote);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DownvoteCommentAsync(Guid commentId, Guid userId)
    {
        // Check if already voted
        var existingVote = await _context.InterviewQuestionCommentVotes
            .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

        if (existingVote == null)
        {
            // Cannot downvote without upvoting first
            return false;
        }

        // If already downvoted, return false (idempotent)
        if (existingVote.VoteType == -1)
        {
            return false;
        }

        // If upvoted, change to downvote
        existingVote.VoteType = -1;
        existingVote.CreatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveDownvoteAsync(Guid commentId, Guid userId)
    {
        var vote = await _context.InterviewQuestionCommentVotes
            .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId && v.VoteType == -1);

        if (vote == null)
        {
            return false; // Not downvoted
        }

        _context.InterviewQuestionCommentVotes.Remove(vote);
        await _context.SaveChangesAsync();
        return true;
    }
}
