using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// DTO for creating a "practice with a friend" interview.
/// This creates a live session immediately (no scheduling / no matching queue).
/// </summary>
public class CreateFriendInterviewDto
{
    /// <summary>
    /// Interview type: data-structures-algorithms, system-design, behavioral, product-management, sql, data-science-ml
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InterviewType { get; set; } = string.Empty;

    /// <summary>
    /// Interview level: beginner, intermediate, advanced
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InterviewLevel { get; set; } = string.Empty;

    /// <summary>
    /// Optional partner email to send the invite link to.
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    public string? PartnerEmail { get; set; }
}

public class FriendInterviewCreatedDto
{
    public Guid LiveSessionId { get; set; }
    public Guid CreatorScheduledSessionId { get; set; }
    public string InterviewType { get; set; } = string.Empty;
    public Guid? ActiveQuestionId { get; set; }

    /// <summary>
    /// Relative URL (frontend route) the creator should navigate to.
    /// </summary>
    public string RedirectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Absolute invite URL that can be shared with a friend.
    /// </summary>
    public string InviteUrl { get; set; } = string.Empty;

    /// <summary>
    /// True only when email delivery is enabled and the send attempt succeeded.
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// Email delivery mode: "sendgrid" or "disabled".
    /// </summary>
    public string EmailDeliveryMode { get; set; } = "disabled";
}

public class JoinFriendInterviewResponseDto
{
    public bool Joined { get; set; }
    public LiveInterviewSessionDto Session { get; set; } = null!;
}

