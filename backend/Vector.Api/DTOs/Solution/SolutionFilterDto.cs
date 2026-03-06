namespace Vector.Api.DTOs.Solution;

public class SolutionFilterDto
{
    public Guid? QuestionId { get; set; }
    public string? Language { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

