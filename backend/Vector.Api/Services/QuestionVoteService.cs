using Microsoft.EntityFrameworkCore;
using Vector.Api.Constants;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class QuestionVoteService : IQuestionVoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ICoinService _coinService;
    private readonly ILogger<QuestionVoteService> _logger;

    public QuestionVoteService(
        ApplicationDbContext context,
        ICoinService coinService,
        ILogger<QuestionVoteService> logger)
    {
        _context = context;
        _coinService = coinService;
        _logger = logger;
    }

    public async Task<int> VoteQuestionAsync(Guid questionId, Guid userId, int voteType)
    {
        if (voteType != 1 && voteType != -1)
        {
            throw new ArgumentException("Vote type must be 1 (upvote) or -1 (downvote)");
        }

        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new KeyNotFoundException("Question not found");
        }

        // Check if user already voted
        var existingVote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId);

        var isNewUpvote = false;

        if (existingVote != null)
        {
            // Update existing vote
            var wasUpvote = existingVote.VoteType == 1;
            existingVote.VoteType = voteType;
            existingVote.UpdatedAt = DateTime.UtcNow;
            
            // Track if this is changing to an upvote
            isNewUpvote = !wasUpvote && voteType == 1;
        }
        else
        {
            // Create new vote
            var vote = new QuestionVote
            {
                QuestionId = questionId,
                UserId = userId,
                VoteType = voteType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.QuestionVotes.Add(vote);
            isNewUpvote = voteType == 1;
        }

        await _context.SaveChangesAsync();

        // Award coins to question creator for upvotes only
        if (isNewUpvote && question.CreatedBy.HasValue && question.CreatedBy.Value != userId)
        {
            try
            {
                await _coinService.AwardCoinsAsync(
                    question.CreatedBy.Value,
                    AchievementTypes.QuestionUpvoted,
                    "Your question was upvoted",
                    questionId,
                    "InterviewQuestion");

                _logger.LogInformation("Awarded QuestionUpvoted coins to user {UserId} for question {QuestionId}",
                    question.CreatedBy.Value, questionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to award QuestionUpvoted coins to user {UserId}", question.CreatedBy.Value);
            }
        }

        // Return total vote count
        return await GetQuestionVoteCountAsync(questionId);
    }

    public async Task<bool> RemoveVoteAsync(Guid questionId, Guid userId)
    {
        var vote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId);

        if (vote == null)
        {
            return false;
        }

        _context.QuestionVotes.Remove(vote);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetQuestionVoteCountAsync(Guid questionId)
    {
        return await _context.QuestionVotes
            .Where(v => v.QuestionId == questionId)
            .SumAsync(v => v.VoteType);
    }

    public async Task<int?> GetUserVoteAsync(Guid questionId, Guid userId)
    {
        var vote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId);

        return vote?.VoteType;
    }
}
