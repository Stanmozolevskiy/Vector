namespace Vector.Api.DTOs.Coins;

/// <summary>
/// DTO for user's total coins and rank
/// </summary>
public class UserCoinsDto
{
    public Guid UserId { get; set; }
    public int TotalCoins { get; set; }
    /// <summary>
    /// Formatted coins display (e.g., "2.3k", "500", "1.5M")
    /// </summary>
    public string DisplayCoins { get; set; } = string.Empty;
    public int? Rank { get; set; }
    /// <summary>
    /// Formatted rank display (e.g., "#108")
    /// </summary>
    public string? DisplayRank { get; set; }
}
