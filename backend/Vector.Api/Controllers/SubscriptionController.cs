using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Api.DTOs.Common;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
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
            return NotFound(new ApiErrorResponse($"Subscription plan '{planId}' not found", "PLAN_NOT_FOUND"));
        }
        return Ok(plan);
    }

    /// <summary>
    /// Get current user's subscription
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(DTOs.Subscription.SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscription()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(userId);
        return Ok(subscription);
    }

    /// <summary>
    /// Update user's subscription plan
    /// </summary>
    [HttpPut("update")]
    [Authorize]
    [ProducesResponseType(typeof(DTOs.Subscription.SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubscription([FromBody] DTOs.Subscription.UpdateSubscriptionDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        try
        {
            var subscription = await _subscriptionService.UpdateSubscriptionAsync(userId, dto.PlanId);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse(ex.Message, "INVALID_PLAN"));
        }
    }

    /// <summary>
    /// Cancel user's subscription
    /// </summary>
    [HttpPut("cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSubscription()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        var result = await _subscriptionService.CancelSubscriptionAsync(userId);
        if (!result)
        {
            return BadRequest(new ApiErrorResponse("Unable to cancel subscription. You may already have a free plan or no active subscription to cancel.", "CANCEL_FAILED"));
        }

        return Ok(new { message = "Subscription cancelled successfully. You have been moved to the free plan." });
    }

    /// <summary>
    /// Get user's billing history/invoices
    /// </summary>
    [HttpGet("invoices")]
    [Authorize]
    [ProducesResponseType(typeof(List<DTOs.Subscription.InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("Invalid or expired authentication token", "INVALID_TOKEN"));
        }

        var invoices = await _subscriptionService.GetInvoicesAsync(userId);
        return Ok(invoices);
    }
}

