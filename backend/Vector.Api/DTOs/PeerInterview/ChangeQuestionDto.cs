namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// Request DTO for changing active question
/// </summary>
public class ChangeQuestionRequestDto
{
    /// <summary>
    /// Optional: Specific question ID to set as active. If not provided, a random question will be selected.
    /// </summary>
    public Guid? QuestionId { get; set; }
}

/// <summary>
/// Response DTO for changing active question
/// </summary>
public class ChangeQuestionResponseDto
{
    public LiveInterviewSessionDto Session { get; set; } = null!;
    public QuestionSummaryDto? NewActiveQuestion { get; set; }
}
