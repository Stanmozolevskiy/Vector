using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Question;

public class QuestionSolutionDto
{
    public Guid Id { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? TimeComplexity { get; set; }
    public string? SpaceComplexity { get; set; }
    public bool IsOfficial { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSolutionDto
{
    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
    
    public string? Explanation { get; set; }
    public string? TimeComplexity { get; set; }
    public string? SpaceComplexity { get; set; }
    public bool IsOfficial { get; set; } = false;
}

