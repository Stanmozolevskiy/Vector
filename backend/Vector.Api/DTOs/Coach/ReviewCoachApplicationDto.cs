using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Coach;

/// <summary>
/// DTO for reviewing (approving/rejecting) a coach application
/// </summary>
public class ReviewCoachApplicationDto
{
    [Required]
    [RegularExpression("^(approved|rejected)$", ErrorMessage = "Status must be either 'approved' or 'rejected'")]
    public string Status { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? AdminNotes { get; set; }
}

