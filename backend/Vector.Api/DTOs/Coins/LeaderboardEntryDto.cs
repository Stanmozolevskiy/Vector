namespace Vector.Api.DTOs.Coins;

/// <summary>
/// DTO for a single leaderboard entry
/// </summary>
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public int TotalCoins { get; set; }
    /// <summary>
    /// Formatted coins display (e.g., "2.3k")
    /// </summary>
    public string DisplayCoins { get; set; } = string.Empty;
}
