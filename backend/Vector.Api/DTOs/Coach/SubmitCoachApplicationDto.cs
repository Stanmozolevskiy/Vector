using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Coach;

/// <summary>
/// DTO for submitting a coach application
/// </summary>
public class SubmitCoachApplicationDto
{
    [Required]
    [MinLength(50)]
    [MaxLength(500)]
    public string Motivation { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Experience { get; set; }
    
    [MaxLength(500)]
    public string? Specialization { get; set; }
    
    /// <summary>
    /// Comma-separated list of image URLs (uploaded separately)
    /// </summary>
    public List<string>? ImageUrls { get; set; }
}

