using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.User;

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public string? Bio { get; set; }
}

