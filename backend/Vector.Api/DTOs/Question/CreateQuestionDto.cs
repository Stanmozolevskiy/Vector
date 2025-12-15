using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Question;

public class CreateQuestionDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Difficulty { get; set; } = "Medium";
    
    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = "Coding";
    
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    public List<string>? CompanyTags { get; set; }
    public List<string>? Tags { get; set; }
    public string? Constraints { get; set; }
    public List<ExampleDto>? Examples { get; set; }
    public List<string>? Hints { get; set; }
    public string? TimeComplexityHint { get; set; }
    public string? SpaceComplexityHint { get; set; }
    public double? AcceptanceRate { get; set; }
}

