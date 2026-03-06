using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a user's total coins and cached rank
/// </summary>
public class UserCoins
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public int TotalCoins { get; set; } = 0;
    
    /// <summary>
    /// Cached rank for performance (updated periodically by background job)
    /// </summary>
    public int? Rank { get; set; }
    
    /// <summary>
    /// Timestamp of last rank update
    /// </summary>
    public DateTime? LastRankUpdate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
