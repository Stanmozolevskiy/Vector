namespace Vector.Api.DTOs.Analytics;

public class LearningAnalyticsDto
{
    public Guid UserId { get; set; }
    public int QuestionsSolved { get; set; }
    public Dictionary<string, int> QuestionsByCategory { get; set; } = new();
    public Dictionary<string, int> QuestionsByDifficulty { get; set; } = new();
    public decimal AverageExecutionTime { get; set; }
    public long AverageMemoryUsed { get; set; }
    public decimal SuccessRate { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int TotalSubmissions { get; set; }
    public Dictionary<string, int> SolutionsByLanguage { get; set; } = new();
}

