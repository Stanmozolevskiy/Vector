namespace Vector.Api.DTOs.Coins;

/// <summary>
/// Request to award coins to a user
/// </summary>
public class AwardCoinsRequest
{
    public Guid UserId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
