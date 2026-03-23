using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.User;

public class UpdateProfileDto
{
    [MaxLength(35, ErrorMessage = "First name must be at most 35 characters.")]
    [RegularExpression(@"^[\p{L}\s\-']+$", ErrorMessage = "First name cannot contain numbers or special characters.")]
    public string? FirstName { get; set; }
    
    [MaxLength(35, ErrorMessage = "Last name must be at most 35 characters.")]
    [RegularExpression(@"^[\p{L}\s\-']+$", ErrorMessage = "Last name cannot contain numbers or special characters.")]
    public string? LastName { get; set; }
    
    public string? Bio { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
}

