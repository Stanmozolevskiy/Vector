using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Question;

public class UpdateQuestionDto
{
    [MaxLength(200)]
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    [MaxLength(20)]
    public string? Difficulty { get; set; }
    
    [MaxLength(50)]
    public string? QuestionType { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    public List<string>? CompanyTags { get; set; }
    public List<string>? Tags { get; set; }
    public string? Constraints { get; set; }
    public List<ExampleDto>? Examples { get; set; }
    public List<string>? Hints { get; set; }
    public string? TimeComplexityHint { get; set; }
    public string? SpaceComplexityHint { get; set; }
    public double? AcceptanceRate { get; set; }
    public bool? IsActive { get; set; }
}

