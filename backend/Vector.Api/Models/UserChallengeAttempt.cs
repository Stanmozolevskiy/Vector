using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vector.Api.Models;

/// <summary>
/// Represents a user's attempt at a daily challenge
/// </summary>
public class UserChallengeAttempt
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the user
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>
    /// Reference to the daily challenge
    /// </summary>
    [Required]
    public Guid ChallengeId { get; set; }

    [ForeignKey(nameof(ChallengeId))]
    public DailyChallenge Challenge { get; set; } = null!;

    /// <summary>
    /// When the user started this challenge
    /// </summary>
    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user completed this challenge (null if not completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether the user successfully completed the challenge
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Time taken to complete in seconds (null if not completed)
    /// </summary>
    public int? TimeSpentSeconds { get; set; }

    /// <summary>
    /// Programming language used
    /// </summary>
    [MaxLength(50)]
    public string? Language { get; set; }

    /// <summary>
    /// User's submitted code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Number of test cases passed
    /// </summary>
    public int TestCasesPassed { get; set; } = 0;

    /// <summary>
    /// Total number of test cases
    /// </summary>
    public int TotalTestCases { get; set; } = 0;

    /// <summary>
    /// Coins earned for completing this challenge
    /// </summary>
    public int CoinsEarned { get; set; } = 0;
}
