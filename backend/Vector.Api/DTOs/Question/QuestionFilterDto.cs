namespace Vector.Api.DTOs.Question;

public class QuestionFilterDto
{
    public string? Search { get; set; }
    public string? QuestionType { get; set; }
    public string? Category { get; set; }
    public List<string>? Categories { get; set; }
    public string? Difficulty { get; set; }
    public List<string>? Difficulties { get; set; }
    public List<string>? Companies { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; } = true;
    public string? ApprovalStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

