using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IRecommendationService
{
    /// <summary>
    /// Get personalized question recommendations based on user's performance
    /// </summary>
    Task<IEnumerable<InterviewQuestion>> GetRecommendedQuestionsAsync(Guid userId, int limit = 10);

    /// <summary>
    /// Get a personalized problem set for the user
    /// </summary>
    Task<object> GetPersonalizedSetAsync(Guid userId);

    /// <summary>
    /// Get questions to improve weak areas
    /// </summary>
    Task<IEnumerable<InterviewQuestion>> GetWeakAreaQuestionsAsync(Guid userId, int limit = 5);
}
