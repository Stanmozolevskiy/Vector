using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a coach application submitted by a user
/// </summary>
public class CoachApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    public string Motivation { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Experience { get; set; }
    
    [MaxLength(500)]
    public string? Specialization { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, approved, rejected
    
    [MaxLength(500)]
    public string? AdminNotes { get; set; }
    
    public Guid? ReviewedBy { get; set; } // Admin user ID who reviewed
    
    public User? Reviewer { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

