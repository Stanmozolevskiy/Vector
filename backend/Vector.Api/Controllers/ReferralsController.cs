using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referralService;
    private readonly ILogger<ReferralsController> _logger;

    public ReferralsController(IReferralService referralService, ILogger<ReferralsController> logger)
    {
        _referralService = referralService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReferral([FromBody] CreateReferralRequest request)
    {
        var userId = GetCurrentUserId();
        var referral = await _referralService.CreateReferralAsync(userId, request.Email);
        
        var referralLink = $"{Request.Scheme}://{Request.Host}/register?ref={referral.ReferralCode}";
        
        return Ok(new 
        { 
            referralCode = referral.ReferralCode,
            referralLink,
            referredEmail = referral.ReferredEmail,
            expiresAt = referral.ExpiresAt
        });
    }

    [HttpGet("my-referrals")]
    public async Task<IActionResult> GetMyReferrals()
    {
        var userId = GetCurrentUserId();
        var referrals = await _referralService.GetUserReferralsAsync(userId);
        return Ok(referrals);
    }

    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCode(string code)
    {
        var referral = await _referralService.GetReferralByCodeAsync(code);
        if (referral == null)
        {
            return NotFound(new { message = "Invalid or expired referral code" });
        }

        return Ok(new 
        { 
            valid = true,
            referrerName = $"{referral.Referrer.FirstName} {referral.Referrer.LastName}".Trim()
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

public class CreateReferralRequest
{
    public string Email { get; set; } = string.Empty;
}
