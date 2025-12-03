using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new 
            { 
                message = "Authentication required" 
            });
            return;
        }

        // Get user role from claims
        var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userRole))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check if user has any of the required roles
        if (!_roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new 
            { 
                message = $"Access denied. Required role(s): {string.Join(", ", _roles)}" 
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }
    }
}

