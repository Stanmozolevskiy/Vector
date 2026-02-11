using Microsoft.EntityFrameworkCore;
using Vector.Api.Constants;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class ReferralService : IReferralService
{
    private readonly ApplicationDbContext _context;
    private readonly ICoinService _coinService;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(
        ApplicationDbContext context,
        ICoinService coinService,
        ILogger<ReferralService> logger)
    {
        _context = context;
        _coinService = coinService;
        _logger = logger;
    }

    public async Task<Referral> CreateReferralAsync(Guid userId, string referredEmail)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(referredEmail) || !referredEmail.Contains("@"))
        {
            throw new ArgumentException("Invalid email address");
        }

        referredEmail = referredEmail.ToLower().Trim();

        // Check if this email was already referred by this user
        var existingReferral = await _context.Referrals
            .FirstOrDefaultAsync(r => r.ReferrerId == userId && 
                                     r.ReferredEmail.ToLower() == referredEmail &&
                                     r.Status != "Expired");

        if (existingReferral != null)
        {
            throw new InvalidOperationException("You have already referred this email address");
        }

        // Generate unique referral code
        var referralCode = GenerateReferralCode();
        
        // Ensure code is unique
        while (await _context.Referrals.AnyAsync(r => r.ReferralCode == referralCode))
        {
            referralCode = GenerateReferralCode();
        }

        var referral = new Referral
        {
            ReferrerId = userId,
            ReferredEmail = referredEmail,
            ReferralCode = referralCode,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Referral code valid for 30 days
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Referrals.Add(referral);
        await _context.SaveChangesAsync();

        return referral;
    }

    public async Task<IEnumerable<Referral>> GetUserReferralsAsync(Guid userId)
    {
        return await _context.Referrals
            .Include(r => r.ReferredUser)
            .Where(r => r.ReferrerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Referral?> GetReferralByCodeAsync(string code)
    {
        return await _context.Referrals
            .Include(r => r.Referrer)
            .FirstOrDefaultAsync(r => r.ReferralCode == code && r.Status == "Pending");
    }

    public async Task<bool> CompleteReferralAsync(Guid referredUserId)
    {
        var user = await _context.Users.FindAsync(referredUserId);
        if (user == null)
        {
            return false;
        }

        // Find referral for this user's email
        var referral = await _context.Referrals
            .FirstOrDefaultAsync(r => r.ReferredEmail.ToLower() == user.Email.ToLower() && 
                                     r.Status == "Pending");

        if (referral == null)
        {
            return false; // No pending referral found
        }

        // Check if expired
        if (referral.ExpiresAt < DateTime.UtcNow)
        {
            referral.Status = "Expired";
            referral.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return false;
        }

        // Update referral
        referral.ReferredUserId = referredUserId;
        referral.Status = "Completed";
        referral.CompletedAt = DateTime.UtcNow;
        referral.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Award coins to referrer
        try
        {
            await _coinService.AwardCoinsAsync(
                referral.ReferrerId,
                AchievementTypes.ReferralSuccess,
                $"Successfully referred {user.Email}",
                referral.Id,
                "Referral");

            _logger.LogInformation("Awarded ReferralSuccess coins to user {UserId} for referring {ReferredUserId}",
                referral.ReferrerId, referredUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to award ReferralSuccess coins to user {UserId}", referral.ReferrerId);
        }

        return true;
    }

    private string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
