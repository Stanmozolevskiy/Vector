using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Auth;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    [RegularExpression(@"^[^0-9]+$", ErrorMessage = "First name must not contain numbers.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    [RegularExpression(@"^[^0-9]+$", ErrorMessage = "Last name must not contain numbers.")]
    public string LastName { get; set; } = string.Empty;
}

