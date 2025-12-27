using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

public class VideoSession
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("PeerInterviewSession")]
    public Guid SessionId { get; set; }
    public PeerInterviewSession Session { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Ended

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    [MaxLength(1000)]
    public string? SignalingData { get; set; } // JSON data for WebRTC signaling
}

