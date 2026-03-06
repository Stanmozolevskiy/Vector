using Vector.Api.DTOs.Coach;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service interface for coach application management
/// </summary>
public interface ICoachService
{
    /// <summary>
    /// Submit a coach application
    /// </summary>
    Task<CoachApplication> SubmitApplicationAsync(Guid userId, SubmitCoachApplicationDto dto);
    
    /// <summary>
    /// Get coach application by user ID
    /// </summary>
    Task<CoachApplication?> GetApplicationByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Get coach application by ID
    /// </summary>
    Task<CoachApplication?> GetApplicationByIdAsync(Guid applicationId);
    
    /// <summary>
    /// Get all pending coach applications (for admin)
    /// </summary>
    Task<List<CoachApplication>> GetPendingApplicationsAsync();
    
    /// <summary>
    /// Get all coach applications (for admin)
    /// </summary>
    Task<List<CoachApplication>> GetAllApplicationsAsync();
    
    /// <summary>
    /// Review (approve/reject) a coach application
    /// </summary>
    Task<CoachApplication> ReviewApplicationAsync(Guid applicationId, Guid adminId, ReviewCoachApplicationDto dto);
}

