namespace Vector.Api.DTOs.Question;

public class InterviewQuestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "Coding";
    public string Category { get; set; } = string.Empty;
    public List<string>? CompanyTags { get; set; }
    public List<string>? Tags { get; set; }
    public string? Constraints { get; set; }
    public List<ExampleDto>? Examples { get; set; }
    public List<string>? Hints { get; set; }
    public string? TimeComplexityHint { get; set; }
    public string? SpaceComplexityHint { get; set; }
    public double? AcceptanceRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ExampleDto
{
    public string? Input { get; set; }
    public string? Output { get; set; }
    public string? Explanation { get; set; }
}

