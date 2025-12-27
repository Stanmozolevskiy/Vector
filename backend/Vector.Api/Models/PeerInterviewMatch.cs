using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

public class PeerInterviewMatch
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(20)]
    public string? PreferredDifficulty { get; set; } // Easy, Medium, Hard, Any

    // JSON array of preferred categories
    public string? PreferredCategories { get; set; } // e.g., ["Arrays", "Strings", "Trees"]

    // JSON array of availability time slots
    // Format: [{"day": "Monday", "startTime": "09:00", "endTime": "17:00"}, ...]
    public string? Availability { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime? LastMatchDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

