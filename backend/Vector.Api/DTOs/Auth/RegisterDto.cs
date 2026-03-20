using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(50)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(30)]
    [RegularExpression(@"^[\p{L}\s\-']+$", ErrorMessage = "First name cannot contain numbers or special characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(30)]
    [RegularExpression(@"^[\p{L}\s\-']+$", ErrorMessage = "Last name cannot contain numbers or special characters.")]
    public string LastName { get; set; } = string.Empty;
}

