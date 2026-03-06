using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class ChallengeService : IChallengeService
{
    private readonly ApplicationDbContext _context;
    private readonly ICoinService _coinService;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        ApplicationDbContext context,
        ICoinService coinService,
        ILogger<ChallengeService> logger)
    {
        _context = context;
        _coinService = coinService;
        _logger = logger;
    }

    public async Task<DailyChallenge?> GetDailyChallengeAsync(DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        
        var challenge = await _context.DailyChallenges
            .Include(c => c.Question)
            .FirstOrDefaultAsync(c => c.Date.Date == targetDate && c.IsActive);

        if (challenge == null)
        {
            _logger.LogInformation("No daily challenge found for date {Date}, creating one", targetDate);
            challenge = await CreateDailyChallengeForDateAsync(targetDate);
        }

        return challenge;
    }

    public async Task<UserChallengeAttempt?> GetUserChallengeAttemptAsync(Guid userId, Guid challengeId)
    {
        return await _context.UserChallengeAttempts
            .Include(a => a.Challenge)
                .ThenInclude(c => c.Question)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ChallengeId == challengeId);
    }

    public async Task<UserChallengeAttempt> StartChallengeAttemptAsync(Guid userId, Guid challengeId)
    {
        // Check if attempt already exists
        var existingAttempt = await GetUserChallengeAttemptAsync(userId, challengeId);
        if (existingAttempt != null)
        {
            return existingAttempt;
        }

        // Create new attempt
        var attempt = new UserChallengeAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChallengeId = challengeId,
            StartedAt = DateTime.UtcNow
        };

        _context.UserChallengeAttempts.Add(attempt);
        
        // Increment attempt count for the challenge
        var challenge = await _context.DailyChallenges.FindAsync(challengeId);
        if (challenge != null)
        {
            challenge.AttemptCount++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} started challenge {ChallengeId}", userId, challengeId);

        return await GetUserChallengeAttemptAsync(userId, challengeId) ?? attempt;
    }

    public async Task<UserChallengeAttempt> CompleteChallengeAttemptAsync(
        Guid userId,
        Guid challengeId,
        string code,
        string language,
        int testCasesPassed,
        int totalTestCases)
    {
        var attempt = await GetUserChallengeAttemptAsync(userId, challengeId);
        if (attempt == null)
        {
            throw new InvalidOperationException("Challenge attempt not found");
        }

        if (attempt.IsCompleted)
        {
            _logger.LogWarning("User {UserId} attempted to complete already completed challenge {ChallengeId}", userId, challengeId);
            return attempt;
        }

        var isFullyCompleted = testCasesPassed == totalTestCases && totalTestCases > 0;

        attempt.CompletedAt = DateTime.UtcNow;
        attempt.IsCompleted = isFullyCompleted;
        attempt.Code = code;
        attempt.Language = language;
        attempt.TestCasesPassed = testCasesPassed;
        attempt.TotalTestCases = totalTestCases;
        attempt.TimeSpentSeconds = (int)(attempt.CompletedAt.Value - attempt.StartedAt).TotalSeconds;

        // Award coins for completing the challenge
        if (isFullyCompleted)
        {
            var coinsToAward = CalculateCoinsForChallenge(attempt.Challenge.Difficulty);
            attempt.CoinsEarned = coinsToAward;

            await _coinService.AwardCoinsAsync(
                userId,
                "DailyChallenge",
                $"Completed daily challenge: {attempt.Challenge.Question.Title}");

            // Increment completion count
            var challenge = await _context.DailyChallenges.FindAsync(challengeId);
            if (challenge != null)
            {
                challenge.CompletionCount++;
            }

            _logger.LogInformation("User {UserId} completed challenge {ChallengeId} and earned {Coins} coins", 
                userId, challengeId, coinsToAward);
        }

        await _context.SaveChangesAsync();

        return attempt;
    }

    public async Task<IEnumerable<UserChallengeAttempt>> GetChallengeHistoryAsync(Guid userId, int limit = 30)
    {
        return await _context.UserChallengeAttempts
            .Include(a => a.Challenge)
                .ThenInclude(c => c.Question)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<object> GetChallengeStatsAsync(Guid userId)
    {
        var attempts = await _context.UserChallengeAttempts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var completedChallenges = attempts.Count(a => a.IsCompleted);
        var totalAttempts = attempts.Count;
        var totalCoinsEarned = attempts.Sum(a => a.CoinsEarned);
        var averageCompletionTime = attempts
            .Where(a => a.IsCompleted && a.TimeSpentSeconds.HasValue)
            .Average(a => (double?)a.TimeSpentSeconds) ?? 0;

        // Calculate current streak
        var currentStreak = await CalculateCurrentStreakAsync(userId);

        // Get longest streak
        var longestStreak = await CalculateLongestStreakAsync(userId);

        return new
        {
            completedChallenges,
            totalAttempts,
            completionRate = totalAttempts > 0 ? (double)completedChallenges / totalAttempts : 0,
            totalCoinsEarned,
            averageCompletionTimeSeconds = (int)averageCompletionTime,
            currentStreak,
            longestStreak
        };
    }

    public async Task<DailyChallenge> CreateOrUpdateDailyChallengeAsync(DateTime date, Guid questionId)
    {
        var targetDate = date.Date;
        var existingChallenge = await _context.DailyChallenges
            .FirstOrDefaultAsync(c => c.Date.Date == targetDate);

        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new InvalidOperationException("Question not found");
        }

        if (existingChallenge != null)
        {
            existingChallenge.QuestionId = questionId;
            existingChallenge.Difficulty = question.Difficulty;
            existingChallenge.Category = question.Category;
        }
        else
        {
            existingChallenge = new DailyChallenge
            {
                Id = Guid.NewGuid(),
                Date = targetDate,
                QuestionId = questionId,
                Difficulty = question.Difficulty,
                Category = question.Category,
                CreatedAt = DateTime.UtcNow
            };
            _context.DailyChallenges.Add(existingChallenge);
        }

        await _context.SaveChangesAsync();

        return existingChallenge;
    }

    public async Task<IEnumerable<DailyChallenge>> GetPastChallengesAsync(int limit = 7)
    {
        return await _context.DailyChallenges
            .Include(c => c.Question)
            .Where(c => c.Date < DateTime.UtcNow.Date && c.IsActive)
            .OrderByDescending(c => c.Date)
            .Take(limit)
            .ToListAsync();
    }

    // Private helper methods

    private async Task<DailyChallenge> CreateDailyChallengeForDateAsync(DateTime date)
    {
        // Get a question that hasn't been used recently
        var recentChallenges = await _context.DailyChallenges
            .Where(c => c.Date >= date.AddDays(-30))
            .Select(c => c.QuestionId)
            .ToListAsync();

        var question = await _context.InterviewQuestions
            .Where(q => !recentChallenges.Contains(q.Id) && q.ApprovalStatus == "Approved")
            .OrderBy(q => Guid.NewGuid()) // Random
            .FirstOrDefaultAsync();

        if (question == null)
        {
            // Fallback to any approved question
            question = await _context.InterviewQuestions
                .Where(q => q.ApprovalStatus == "Approved")
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefaultAsync();
        }

        if (question == null)
        {
            throw new InvalidOperationException("No approved questions available for daily challenge");
        }

        return await CreateOrUpdateDailyChallengeAsync(date, question.Id);
    }

    private int CalculateCoinsForChallenge(string difficulty)
    {
        return difficulty.ToLower() switch
        {
            "easy" => 10,
            "medium" => 20,
            "hard" => 30,
            _ => 10
        };
    }

    private async Task<int> CalculateCurrentStreakAsync(Guid userId)
    {
        var attempts = await _context.UserChallengeAttempts
            .Include(a => a.Challenge)
            .Where(a => a.UserId == userId && a.IsCompleted)
            .OrderByDescending(a => a.Challenge.Date)
            .Select(a => a.Challenge.Date.Date)
            .Distinct()
            .ToListAsync();

        if (!attempts.Any())
        {
            return 0;
        }

        int streak = 0;
        var currentDate = DateTime.UtcNow.Date;

        foreach (var attemptDate in attempts)
        {
            if (attemptDate == currentDate || attemptDate == currentDate.AddDays(-streak - 1))
            {
                streak++;
                currentDate = attemptDate;
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    private async Task<int> CalculateLongestStreakAsync(Guid userId)
    {
        var attempts = await _context.UserChallengeAttempts
            .Include(a => a.Challenge)
            .Where(a => a.UserId == userId && a.IsCompleted)
            .OrderBy(a => a.Challenge.Date)
            .Select(a => a.Challenge.Date.Date)
            .Distinct()
            .ToListAsync();

        if (!attempts.Any())
        {
            return 0;
        }

        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < attempts.Count; i++)
        {
            if (attempts[i] == attempts[i - 1].AddDays(1))
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }
}
