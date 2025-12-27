namespace Vector.Api.DTOs.Question;

public class QuestionListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "Coding";
    public string Category { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public List<string>? CompanyTags { get; set; }
    public double? AcceptanceRate { get; set; }
    public bool IsActive { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
}

