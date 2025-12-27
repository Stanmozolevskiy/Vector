using Vector.Api.DTOs.Analytics;

namespace Vector.Api.Services;

public interface IAnalyticsService
{
    /// <summary>
    /// Update analytics when a solution is submitted
    /// </summary>
    Task UpdateAnalyticsAsync(Guid userId, Guid questionId, string status, decimal executionTime, long memoryUsed, string language);
    
    /// <summary>
    /// Get user analytics
    /// </summary>
    Task<LearningAnalyticsDto> GetUserAnalyticsAsync(Guid userId);
    
    /// <summary>
    /// Get progress for a specific category
    /// </summary>
    Task<CategoryProgressDto?> GetCategoryProgressAsync(Guid userId, string category);
    
    /// <summary>
    /// Get progress for a specific difficulty level
    /// </summary>
    Task<DifficultyProgressDto?> GetDifficultyProgressAsync(Guid userId, string difficulty);
    
    /// <summary>
    /// Calculate and update streak for a user
    /// </summary>
    Task CalculateStreakAsync(Guid userId);
}

