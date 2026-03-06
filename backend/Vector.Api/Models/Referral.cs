using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents a user referral
/// </summary>
public class Referral
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User who sent the referral
    /// </summary>
    [Required]
    public Guid ReferrerId { get; set; }
    
    public User Referrer { get; set; } = null!;
    
    /// <summary>
    /// User who was referred (optional - may not have signed up yet)
    /// </summary>
    public Guid? ReferredUserId { get; set; }
    
    public User? ReferredUser { get; set; }
    
    /// <summary>
    /// Email of the referred person
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string ReferredEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique referral code/token
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ReferralCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Status: Pending, Completed, Expired
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// When the referred user completed their first interview (triggers reward)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// When the referral code expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
