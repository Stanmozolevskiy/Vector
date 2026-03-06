using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IReferralService
{
    Task<Referral> CreateReferralAsync(Guid userId, string referredEmail);
    Task<IEnumerable<Referral>> GetUserReferralsAsync(Guid userId);
    Task<Referral?> GetReferralByCodeAsync(string code);
    Task<bool> CompleteReferralAsync(Guid referredUserId);
}
