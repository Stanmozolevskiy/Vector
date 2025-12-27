namespace Vector.Api.DTOs.Analytics;

public class CategoryProgressDto
{
    public string Category { get; set; } = string.Empty;
    public int QuestionsSolved { get; set; }
    public int TotalQuestions { get; set; }
    public decimal CompletionPercentage { get; set; }
    public decimal AverageExecutionTime { get; set; }
    public long AverageMemoryUsed { get; set; }
}

