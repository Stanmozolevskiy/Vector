namespace Vector.Api.DTOs.PeerInterview;

/// <summary>
/// Response DTO for role switch operation
/// </summary>
public class SwitchRolesResponseDto
{
    public LiveInterviewSessionDto Session { get; set; } = null!;
    public string YourNewRole { get; set; } = string.Empty; // Interviewer or Interviewee
    public string PartnerNewRole { get; set; } = string.Empty;
}

