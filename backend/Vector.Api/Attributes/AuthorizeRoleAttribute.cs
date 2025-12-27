using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Vector.Api.Attributes;

/// <summary>
/// Authorization attribute that checks if the user has the required role(s)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public AuthorizeRoleAttribute(params string[] roles)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetService<ILogger<AuthorizeRoleAttribute>>();
        
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            logger?.LogWarning("Authorization failed: User not authenticated. Path: {Path}", context.HttpContext.Request.Path);
            context.Result = new UnauthorizedObjectResult(new 
            { 
                message = "Authentication required" 
            });
            return;
        }

        // Get user role from claims
        // JWT tokens may use either ClaimTypes.Role or "role" as the claim name
        var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value
            ?? context.HttpContext.User.FindFirst("role")?.Value;

        if (string.IsNullOrEmpty(userRole))
        {
            // Log available claims for debugging
            var allClaims = context.HttpContext.User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            logger?.LogWarning("Authorization failed: User role not found in token. Path: {Path}, Available claims: {Claims}", 
                context.HttpContext.Request.Path, string.Join(", ", allClaims));
            context.Result = new ObjectResult(new 
            { 
                message = "User role not found in token",
                availableClaims = allClaims
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Check if user has any of the required roles
        if (!_roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            logger?.LogWarning("Authorization failed: User role '{UserRole}' does not match required roles: {RequiredRoles}. Path: {Path}", 
                userRole, string.Join(", ", _roles), context.HttpContext.Request.Path);
            context.Result = new ObjectResult(new 
            { 
                message = $"Access denied. Required role(s): {string.Join(", ", _roles)}. Your role: {userRole}" 
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        logger?.LogDebug("Authorization successful: User role '{UserRole}' matches required roles: {RequiredRoles}. Path: {Path}", 
            userRole, string.Join(", ", _roles), context.HttpContext.Request.Path);
    }
}

