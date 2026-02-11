namespace Vector.Api.DTOs.Coins;

/// <summary>
/// DTO for an achievement definition (how to earn coins)
/// </summary>
public class AchievementDefinitionDto
{
    public string ActivityType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CoinsAwarded { get; set; }
    public string? Icon { get; set; }
}
