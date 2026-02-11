namespace Vector.Api.DTOs.Coins;

/// <summary>
/// DTO for a single coin transaction
/// </summary>
public class CoinTransactionDto
{
    public Guid Id { get; set; }
    public int Amount { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Formatted relative time (e.g., "2h ago", "1d ago", "just now")
    /// </summary>
    public string TimeAgo { get; set; } = string.Empty;
}
