using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Solution;

public class CodeDraftDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class SaveCodeDraftDto
{
    [Required]
    public Guid QuestionId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Language { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
}

