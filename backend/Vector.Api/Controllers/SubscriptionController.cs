using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<DTOs.Subscription.SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(plans);
    }

    /// <summary>
    /// Get a specific subscription plan by ID
    /// </summary>
    [HttpGet("plans/{planId}")]
    [ProducesResponseType(typeof(DTOs.Subscription.SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan(string planId)
    {
        var plan = await _subscriptionService.GetPlanByIdAsync(planId);
        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }
        return Ok(plan);
    }

    // TODO: Implement remaining endpoints
    // - GET /api/subscriptions/me (requires auth)
    // - POST /api/subscriptions/subscribe (requires auth)
    // - PUT /api/subscriptions/cancel (requires auth)
    // - GET /api/subscriptions/invoices (requires auth)
}

