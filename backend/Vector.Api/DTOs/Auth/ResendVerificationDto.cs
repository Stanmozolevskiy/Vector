using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Auth;

public class ResendVerificationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

