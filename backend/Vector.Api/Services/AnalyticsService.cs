using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vector.Api.Data;
using Vector.Api.DTOs.Analytics;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        ApplicationDbContext context,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateAnalyticsAsync(
        Guid userId, 
        Guid questionId, 
        string status, 
        decimal executionTime, 
        long memoryUsed, 
        string language)
    {
        try
        {
            // Get or create analytics record
            var analytics = await _context.LearningAnalytics
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (analytics == null)
            {
                analytics = new LearningAnalytics
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.LearningAnalytics.Add(analytics);
            }

            // Get question details
            var question = await _context.InterviewQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                _logger.LogWarning("Question {QuestionId} not found for analytics update", questionId);
                return;
            }

            // Update total submissions
            analytics.TotalSubmissions++;

            // If solution is accepted, update solved questions
            if (status == "Accepted")
            {
                // Check if this is the first accepted solution for this question
                // Count how many accepted solutions exist for this question
                var acceptedCount = await _context.UserSolutions
                    .CountAsync(s => s.UserId == userId && s.QuestionId == questionId && s.Status == "Accepted");

                // If this is the first accepted solution, increment questions solved
                // The current solution has already been saved, so count should be >= 1
                if (acceptedCount == 1)
                {
                    analytics.QuestionsSolved++;
                }

                // Update category statistics
                UpdateCategoryStats(analytics, question.Category);

                // Update difficulty statistics
                UpdateDifficultyStats(analytics, question.Difficulty);

                // Update language statistics
                UpdateLanguageStats(analytics, language);

                // Update execution metrics (only for accepted solutions)
                UpdateExecutionMetrics(analytics, executionTime, memoryUsed);

                // Update last activity date
                analytics.LastActivityDate = DateTime.UtcNow;

                // Recalculate streak
                await CalculateStreakAsync(userId);
            }

            // Recalculate success rate
            if (analytics.TotalSubmissions > 0)
            {
                var acceptedCount = await _context.UserSolutions
                    .CountAsync(s => s.UserId == userId && s.Status == "Accepted");
                analytics.SuccessRate = (decimal)acceptedCount / analytics.TotalSubmissions * 100;
            }

            analytics.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating analytics for user {UserId}", userId);
            // Don't throw - analytics update failure shouldn't break solution submission
        }
    }

    public async Task<LearningAnalyticsDto> GetUserAnalyticsAsync(Guid userId)
    {
        var analytics = await _context.LearningAnalytics
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (analytics == null)
        {
            // Return empty analytics if none exists with mock interviews count
            var mockInterviews = await _context.ScheduledInterviewSessions
                .CountAsync(s => s.UserId == userId && 
                               s.Status == "Completed" && 
                               s.PracticeType != "friend");

            // Get total question counts by difficulty
            var totalQuestionsByDifficulty = await _context.InterviewQuestions
                .Where(q => q.IsActive && q.QuestionType == "Coding")
                .GroupBy(q => q.Difficulty)
                .Select(g => new { Difficulty = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Difficulty, x => x.Count);

            return new LearningAnalyticsDto
            {
                UserId = userId,
                MockInterviewsCompleted = mockInterviews,
                TotalQuestionsByDifficulty = totalQuestionsByDifficulty
            };
        }

        return await MapToDtoAsync(analytics);
    }

    public async Task<CategoryProgressDto?> GetCategoryProgressAsync(Guid userId, string category)
    {
        var analytics = await _context.LearningAnalytics
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (analytics == null)
        {
            return null;
        }

        var questionsByCategory = ParseJsonDictionary(analytics.QuestionsByCategory);
        var solvedCount = questionsByCategory.TryGetValue(category, out var count) ? count : 0;

        // Get total questions in this category
        var totalQuestions = await _context.InterviewQuestions
            .CountAsync(q => q.Category == category && q.IsActive && q.ApprovalStatus == "Approved");

        // Get average execution metrics for this category
        var categorySolutions = await _context.UserSolutions
            .Include(s => s.Question)
            .Where(s => s.UserId == userId && 
                       s.Status == "Accepted" && 
                       s.Question.Category == category)
            .ToListAsync();

        var avgExecutionTime = categorySolutions.Any() 
            ? categorySolutions.Average(s => (double)s.ExecutionTime) 
            : 0;
        var avgMemory = categorySolutions.Any() 
            ? (long)categorySolutions.Average(s => (double)s.MemoryUsed) 
            : 0;

        return new CategoryProgressDto
        {
            Category = category,
            QuestionsSolved = solvedCount,
            TotalQuestions = totalQuestions,
            CompletionPercentage = totalQuestions > 0 ? (decimal)solvedCount / totalQuestions * 100 : 0,
            AverageExecutionTime = (decimal)avgExecutionTime,
            AverageMemoryUsed = avgMemory
        };
    }

    public async Task<DifficultyProgressDto?> GetDifficultyProgressAsync(Guid userId, string difficulty)
    {
        var analytics = await _context.LearningAnalytics
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (analytics == null)
        {
            return null;
        }

        var questionsByDifficulty = ParseJsonDictionary(analytics.QuestionsByDifficulty);
        var solvedCount = questionsByDifficulty.TryGetValue(difficulty, out var count) ? count : 0;

        // Get total questions in this difficulty
        var totalQuestions = await _context.InterviewQuestions
            .CountAsync(q => q.Difficulty == difficulty && q.IsActive && q.ApprovalStatus == "Approved");

        // Get average execution metrics for this difficulty
        var difficultySolutions = await _context.UserSolutions
            .Include(s => s.Question)
            .Where(s => s.UserId == userId && 
                       s.Status == "Accepted" && 
                       s.Question.Difficulty == difficulty)
            .ToListAsync();

        var avgExecutionTime = difficultySolutions.Any() 
            ? difficultySolutions.Average(s => (double)s.ExecutionTime) 
            : 0;
        var avgMemory = difficultySolutions.Any() 
            ? (long)difficultySolutions.Average(s => (double)s.MemoryUsed) 
            : 0;

        return new DifficultyProgressDto
        {
            Difficulty = difficulty,
            QuestionsSolved = solvedCount,
            TotalQuestions = totalQuestions,
            CompletionPercentage = totalQuestions > 0 ? (decimal)solvedCount / totalQuestions * 100 : 0,
            AverageExecutionTime = (decimal)avgExecutionTime,
            AverageMemoryUsed = avgMemory
        };
    }

    public async Task CalculateStreakAsync(Guid userId)
    {
        try
        {
            var analytics = await _context.LearningAnalytics
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (analytics == null)
            {
                return;
            }

            // Get all solved questions ordered by date (using UserSolvedQuestions)
            var solvedDates = await _context.UserSolvedQuestions
                .Where(usq => usq.UserId == userId)
                .OrderByDescending(usq => usq.SolvedAt)
                .Select(usq => usq.SolvedAt.Date)
                .Distinct()
                .ToListAsync();

            if (solvedDates.Count == 0)
            {
                analytics.CurrentStreak = 0;
                analytics.LongestStreak = 0;
                analytics.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            // Calculate current streak
            var currentStreak = 0;
            var today = DateTime.UtcNow.Date;
            var checkDate = today;

            // If last activity was today or yesterday, start counting
            if (solvedDates.Contains(today) || solvedDates.Contains(today.AddDays(-1)))
            {
                foreach (var date in solvedDates.OrderByDescending(d => d))
                {
                    if (date == checkDate || date == checkDate.AddDays(-1))
                    {
                        currentStreak++;
                        checkDate = date.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Calculate longest streak
            var longestStreak = 1;
            var currentRun = 1;

            for (int i = 1; i < solvedDates.Count; i++)
            {
                var daysDiff = (solvedDates[i - 1] - solvedDates[i]).Days;
                if (daysDiff == 1)
                {
                    currentRun++;
                    longestStreak = Math.Max(longestStreak, currentRun);
                }
                else
                {
                    currentRun = 1;
                }
            }

            analytics.CurrentStreak = currentStreak;
            analytics.LongestStreak = Math.Max(analytics.LongestStreak, longestStreak);
            analytics.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating streak for user {UserId}", userId);
        }
    }

    private void UpdateCategoryStats(LearningAnalytics analytics, string category)
    {
        var categoryDict = ParseJsonDictionary(analytics.QuestionsByCategory);
        categoryDict[category] = categoryDict.GetValueOrDefault(category, 0) + 1;
        analytics.QuestionsByCategory = JsonSerializer.Serialize(categoryDict);
    }

    private void UpdateDifficultyStats(LearningAnalytics analytics, string difficulty)
    {
        var difficultyDict = ParseJsonDictionary(analytics.QuestionsByDifficulty);
        difficultyDict[difficulty] = difficultyDict.GetValueOrDefault(difficulty, 0) + 1;
        analytics.QuestionsByDifficulty = JsonSerializer.Serialize(difficultyDict);
    }

    private void UpdateLanguageStats(LearningAnalytics analytics, string language)
    {
        var languageDict = ParseJsonDictionary(analytics.SolutionsByLanguage);
        languageDict[language] = languageDict.GetValueOrDefault(language, 0) + 1;
        analytics.SolutionsByLanguage = JsonSerializer.Serialize(languageDict);
    }

    private void UpdateExecutionMetrics(LearningAnalytics analytics, decimal executionTime, long memoryUsed)
    {
        // Calculate new average
        var currentCount = analytics.QuestionsSolved;
        if (currentCount == 0)
        {
            analytics.AverageExecutionTime = executionTime;
            analytics.AverageMemoryUsed = memoryUsed;
        }
        else
        {
            // Weighted average: (old_avg * (n-1) + new_value) / n
            analytics.AverageExecutionTime = (analytics.AverageExecutionTime * (currentCount - 1) + executionTime) / currentCount;
            analytics.AverageMemoryUsed = (long)((analytics.AverageMemoryUsed * (currentCount - 1) + memoryUsed) / (double)currentCount);
        }
    }

    private Dictionary<string, int> ParseJsonDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, int>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    private async Task<LearningAnalyticsDto> MapToDtoAsync(LearningAnalytics analytics)
    {
        // Count completed mock interviews (excluding practice with friend)
        var mockInterviewsCompleted = await _context.ScheduledInterviewSessions
            .CountAsync(s => s.UserId == analytics.UserId && 
                           s.Status == "Completed" && 
                           s.PracticeType != "friend");

        // Get total question counts by difficulty (only coding questions that are active)
        var totalQuestionsByDifficulty = await _context.InterviewQuestions
            .Where(q => q.IsActive && q.QuestionType == "Coding")
            .GroupBy(q => q.Difficulty)
            .Select(g => new { Difficulty = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Difficulty, x => x.Count);

        return new LearningAnalyticsDto
        {
            UserId = analytics.UserId,
            QuestionsSolved = analytics.QuestionsSolved,
            QuestionsByCategory = ParseJsonDictionary(analytics.QuestionsByCategory),
            QuestionsByDifficulty = ParseJsonDictionary(analytics.QuestionsByDifficulty),
            TotalQuestionsByDifficulty = totalQuestionsByDifficulty,
            AverageExecutionTime = analytics.AverageExecutionTime,
            AverageMemoryUsed = analytics.AverageMemoryUsed,
            SuccessRate = analytics.SuccessRate,
            CurrentStreak = analytics.CurrentStreak,
            LongestStreak = analytics.LongestStreak,
            LastActivityDate = analytics.LastActivityDate,
            TotalSubmissions = analytics.TotalSubmissions,
            SolutionsByLanguage = ParseJsonDictionary(analytics.SolutionsByLanguage),
            MockInterviewsCompleted = mockInterviewsCompleted
        };
    }

    public async Task RebuildAnalyticsAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Rebuilding analytics for user {UserId}", userId);

            // Get or create analytics record
            var analytics = await _context.LearningAnalytics
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (analytics == null)
            {
                analytics = new LearningAnalytics
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.LearningAnalytics.Add(analytics);
            }

            // Get all solved questions from UserSolvedQuestions table (the source of truth for checkmarks)
            var solvedQuestions = await _context.UserSolvedQuestions
                .Include(usq => usq.Question)
                .Where(usq => usq.UserId == userId)
                .ToListAsync();

            _logger.LogInformation("Found {Count} solved questions for user {UserId}", solvedQuestions.Count, userId);

            // Count unique questions solved
            analytics.QuestionsSolved = solvedQuestions.Count;

            // Reset dictionaries
            var categoryDict = new Dictionary<string, int>();
            var difficultyDict = new Dictionary<string, int>();
            var languageDict = new Dictionary<string, int>();

            // Count by category, difficulty, and language
            foreach (var solved in solvedQuestions)
            {
                if (solved.Question != null)
                {
                    var category = solved.Question.Category;
                    var difficulty = solved.Question.Difficulty;
                    var language = solved.Language ?? "Unknown";

                    categoryDict[category] = categoryDict.GetValueOrDefault(category, 0) + 1;
                    difficultyDict[difficulty] = difficultyDict.GetValueOrDefault(difficulty, 0) + 1;
                    languageDict[language] = languageDict.GetValueOrDefault(language, 0) + 1;
                }
            }

            analytics.QuestionsByCategory = JsonSerializer.Serialize(categoryDict);
            analytics.QuestionsByDifficulty = JsonSerializer.Serialize(difficultyDict);
            analytics.SolutionsByLanguage = JsonSerializer.Serialize(languageDict);

            _logger.LogInformation("Category breakdown: {Categories}", analytics.QuestionsByCategory);
            _logger.LogInformation("Difficulty breakdown: {Difficulties}", analytics.QuestionsByDifficulty);

            // Set last activity date
            if (solvedQuestions.Any())
            {
                analytics.LastActivityDate = solvedQuestions.Max(s => s.SolvedAt);
            }

            // Get total submissions from UserSolutions (if available)
            analytics.TotalSubmissions = await _context.UserSolutions
                .CountAsync(s => s.UserId == userId);

            // Calculate success rate based on actual accepted solutions
            var acceptedSolutionsCount = await _context.UserSolutions
                .CountAsync(s => s.UserId == userId && s.Status == "Accepted");
            
            if (analytics.TotalSubmissions > 0)
            {
                analytics.SuccessRate = (decimal)acceptedSolutionsCount / analytics.TotalSubmissions * 100;
            }
            else if (solvedQuestions.Any())
            {
                // If no UserSolutions but have UserSolvedQuestions, assume 100% success
                analytics.SuccessRate = 100;
                analytics.TotalSubmissions = solvedQuestions.Count;
            }

            // Calculate streak
            await CalculateStreakAsync(userId);

            // Save all changes including category/difficulty/language stats
            analytics.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Analytics rebuilt successfully for user {UserId}: {QuestionsSolved} questions solved", 
                userId, analytics.QuestionsSolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding analytics for user {UserId}", userId);
            throw;
        }
    }
}

