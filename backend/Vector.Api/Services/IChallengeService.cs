using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IChallengeService
{
    /// <summary>
    /// Get the daily challenge for a specific date (defaults to today)
    /// </summary>
    Task<DailyChallenge?> GetDailyChallengeAsync(DateTime? date = null);

    /// <summary>
    /// Get user's challenge attempt for a specific challenge
    /// </summary>
    Task<UserChallengeAttempt?> GetUserChallengeAttemptAsync(Guid userId, Guid challengeId);

    /// <summary>
    /// Start a challenge attempt for a user
    /// </summary>
    Task<UserChallengeAttempt> StartChallengeAttemptAsync(Guid userId, Guid challengeId);

    /// <summary>
    /// Complete a challenge attempt
    /// </summary>
    Task<UserChallengeAttempt> CompleteChallengeAttemptAsync(
        Guid userId, 
        Guid challengeId, 
        string code, 
        string language,
        int testCasesPassed, 
        int totalTestCases);

    /// <summary>
    /// Get user's challenge history
    /// </summary>
    Task<IEnumerable<UserChallengeAttempt>> GetChallengeHistoryAsync(Guid userId, int limit = 30);

    /// <summary>
    /// Get challenge statistics
    /// </summary>
    Task<object> GetChallengeStatsAsync(Guid userId);

    /// <summary>
    /// Create or update daily challenge for a date
    /// </summary>
    Task<DailyChallenge> CreateOrUpdateDailyChallengeAsync(DateTime date, Guid questionId);

    /// <summary>
    /// Get all past challenges
    /// </summary>
    Task<IEnumerable<DailyChallenge>> GetPastChallengesAsync(int limit = 7);
}
