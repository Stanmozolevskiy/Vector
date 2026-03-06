using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        ApplicationDbContext context,
        ILogger<RecommendationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<InterviewQuestion>> GetRecommendedQuestionsAsync(Guid userId, int limit = 10)
    {
        _logger.LogInformation("Getting recommended questions for user {UserId}", userId);

        // Get user's solved questions
        var solvedQuestionIds = await _context.UserSolvedQuestions
            .Where(usq => usq.UserId == userId)
            .Select(usq => usq.QuestionId)
            .ToListAsync();

        // Get user's performance analytics
        var analytics = await GetUserAnalyticsAsync(userId);

        // Determine recommended difficulty
        var recommendedDifficulty = DetermineRecommendedDifficulty(analytics);

        // Get weak categories
        var weakCategories = GetWeakCategories(analytics);

        var recommendations = new List<InterviewQuestion>();

        // First priority: Questions in weak categories
        if (weakCategories.Any())
        {
            var weakCategoryQuestions = await _context.InterviewQuestions
                .Where(q => q.ApprovalStatus == "Approved" 
                    && !solvedQuestionIds.Contains(q.Id)
                    && weakCategories.Contains(q.Category)
                    && q.Difficulty == recommendedDifficulty)
                .OrderBy(q => Guid.NewGuid())
                .Take(limit / 2)
                .ToListAsync();

            recommendations.AddRange(weakCategoryQuestions);
        }

        // Second priority: Questions of appropriate difficulty
        var remainingCount = limit - recommendations.Count;
        if (remainingCount > 0)
        {
            var difficultyQuestions = await _context.InterviewQuestions
                .Where(q => q.ApprovalStatus == "Approved" 
                    && !solvedQuestionIds.Contains(q.Id)
                    && !recommendations.Select(r => r.Id).Contains(q.Id)
                    && q.Difficulty == recommendedDifficulty)
                .OrderBy(q => Guid.NewGuid())
                .Take(remainingCount)
                .ToListAsync();

            recommendations.AddRange(difficultyQuestions);
        }

        // Fill remaining with any unsolved questions
        remainingCount = limit - recommendations.Count;
        if (remainingCount > 0)
        {
            var anyQuestions = await _context.InterviewQuestions
                .Where(q => q.ApprovalStatus == "Approved" 
                    && !solvedQuestionIds.Contains(q.Id)
                    && !recommendations.Select(r => r.Id).Contains(q.Id))
                .OrderBy(q => Guid.NewGuid())
                .Take(remainingCount)
                .ToListAsync();

            recommendations.AddRange(anyQuestions);
        }

        _logger.LogInformation("Returning {Count} recommended questions for user {UserId}", 
            recommendations.Count, userId);

        return recommendations;
    }

    public async Task<object> GetPersonalizedSetAsync(Guid userId)
    {
        _logger.LogInformation("Getting personalized problem set for user {UserId}", userId);

        var analytics = await GetUserAnalyticsAsync(userId);
        var recommendedQuestions = await GetRecommendedQuestionsAsync(userId, 10);
        var weakAreaQuestions = await GetWeakAreaQuestionsAsync(userId, 5);

        return new
        {
            recommendedDifficulty = DetermineRecommendedDifficulty(analytics),
            weakCategories = GetWeakCategories(analytics),
            recommendedQuestions,
            weakAreaQuestions,
            analytics = new
            {
                totalSolved = analytics.TotalSolved,
                easySolved = analytics.EasySolved,
                mediumSolved = analytics.MediumSolved,
                hardSolved = analytics.HardSolved,
                accuracyRate = analytics.AccuracyRate
            }
        };
    }

    public async Task<IEnumerable<InterviewQuestion>> GetWeakAreaQuestionsAsync(Guid userId, int limit = 5)
    {
        var analytics = await GetUserAnalyticsAsync(userId);
        var weakCategories = GetWeakCategories(analytics);

        if (!weakCategories.Any())
        {
            return new List<InterviewQuestion>();
        }

        var solvedQuestionIds = await _context.UserSolvedQuestions
            .Where(usq => usq.UserId == userId)
            .Select(usq => usq.QuestionId)
            .ToListAsync();

        return await _context.InterviewQuestions
            .Where(q => q.ApprovalStatus == "Approved" 
                && !solvedQuestionIds.Contains(q.Id)
                && weakCategories.Contains(q.Category))
            .OrderBy(q => Guid.NewGuid())
            .Take(limit)
            .ToListAsync();
    }

    // Private helper methods

    private async Task<UserAnalytics> GetUserAnalyticsAsync(Guid userId)
    {
        var solvedQuestions = await _context.UserSolvedQuestions
            .Include(usq => usq.Question)
            .Where(usq => usq.UserId == userId)
            .ToListAsync();

        var totalSolved = solvedQuestions.Count;
        var easySolved = solvedQuestions.Count(sq => sq.Question.Difficulty == "Easy");
        var mediumSolved = solvedQuestions.Count(sq => sq.Question.Difficulty == "Medium");
        var hardSolved = solvedQuestions.Count(sq => sq.Question.Difficulty == "Hard");

        // Calculate category performance
        var categoryPerformance = solvedQuestions
            .GroupBy(sq => sq.Question.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategoryPerformance
                {
                    Category = g.Key,
                    TotalAttempts = g.Count(),
                    TotalSolved = g.Count()
                });

        // Calculate overall accuracy (simplified - based on solved vs total attempts)
        var totalAttempts = await _context.UserSolutions
            .Where(us => us.UserId == userId)
            .CountAsync();

        var accuracyRate = totalAttempts > 0 ? (double)totalSolved / totalAttempts : 0;

        return new UserAnalytics
        {
            TotalSolved = totalSolved,
            EasySolved = easySolved,
            MediumSolved = mediumSolved,
            HardSolved = hardSolved,
            CategoryPerformance = categoryPerformance,
            AccuracyRate = accuracyRate
        };
    }

    private string DetermineRecommendedDifficulty(UserAnalytics analytics)
    {
        // If user has solved very few problems, start with Easy
        if (analytics.TotalSolved < 5)
        {
            return "Easy";
        }

        // If user has mastered Easy (solved 10+), move to Medium
        if (analytics.EasySolved >= 10 && analytics.MediumSolved < 10)
        {
            return "Medium";
        }

        // If user has mastered Medium (solved 10+), suggest Hard
        if (analytics.MediumSolved >= 10)
        {
            return "Hard";
        }

        // Default to Medium for intermediate users
        return "Medium";
    }

    private List<string> GetWeakCategories(UserAnalytics analytics)
    {
        if (!analytics.CategoryPerformance.Any())
        {
            return new List<string>();
        }

        // Find categories with below-average performance
        var avgPerformance = analytics.CategoryPerformance.Values
            .Average(cp => cp.TotalSolved);

        return analytics.CategoryPerformance
            .Where(kvp => kvp.Value.TotalSolved < avgPerformance && kvp.Value.TotalSolved > 0)
            .OrderBy(kvp => kvp.Value.TotalSolved)
            .Select(kvp => kvp.Key)
            .Take(3)
            .ToList();
    }

    // Helper classes
    private class UserAnalytics
    {
        public int TotalSolved { get; set; }
        public int EasySolved { get; set; }
        public int MediumSolved { get; set; }
        public int HardSolved { get; set; }
        public Dictionary<string, CategoryPerformance> CategoryPerformance { get; set; } = new();
        public double AccuracyRate { get; set; }
    }

    private class CategoryPerformance
    {
        public string Category { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int TotalSolved { get; set; }
    }
}
