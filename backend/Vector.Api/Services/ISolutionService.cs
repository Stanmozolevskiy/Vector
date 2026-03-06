using Vector.Api.DTOs.Solution;

namespace Vector.Api.Services;

public interface ISolutionService
{
    /// <summary>
    /// Submit a solution for a question (only called when validation passes)
    /// </summary>
    Task SubmitSolutionAsync(Guid userId, SubmitSolutionDto dto);
    
    /// <summary>
    /// Get user's solutions
    /// </summary>
    Task<(List<UserSolutionDto> Solutions, int TotalCount)> GetUserSolutionsAsync(
        Guid userId, 
        SolutionFilterDto? filter = null);
    
    /// <summary>
    /// Get solution by ID
    /// </summary>
    Task<UserSolutionDto?> GetSolutionByIdAsync(Guid solutionId, Guid userId);
    
    /// <summary>
    /// Get solutions for a specific question
    /// </summary>
    Task<List<UserSolutionDto>> GetSolutionsForQuestionAsync(Guid questionId, Guid userId);
    
    /// <summary>
    /// Get solution statistics for a user
    /// </summary>
    Task<SolutionStatisticsDto> GetSolutionStatisticsAsync(Guid userId);
    
    /// <summary>
    /// Check if a user has solved a specific question (optimized lookup)
    /// </summary>
    Task<bool> HasUserSolvedQuestionAsync(Guid userId, Guid questionId);
}

/// <summary>
/// Solution statistics DTO
/// </summary>
public class SolutionStatisticsDto
{
    public int TotalSubmissions { get; set; }
    public int AcceptedSolutions { get; set; }
    public int QuestionsSolved { get; set; }
    public decimal AverageExecutionTime { get; set; }
    public long AverageMemoryUsed { get; set; }
    public Dictionary<string, int> SolutionsByLanguage { get; set; } = new();
    public Dictionary<string, int> SolutionsByStatus { get; set; } = new();
}

