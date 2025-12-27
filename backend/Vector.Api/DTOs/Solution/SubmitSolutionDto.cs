using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Solution;

public class SubmitSolutionDto
{
    [Required]
    public Guid QuestionId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Language { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
}

