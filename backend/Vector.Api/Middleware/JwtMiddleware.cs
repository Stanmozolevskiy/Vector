using Vector.Api.Services;

namespace Vector.Api.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            var userId = jwtService.GetUserIdFromToken(token);
            if (userId != null)
            {
                // Attach user ID to context for use in controllers
                context.Items["UserId"] = userId;
            }
        }

        await _next(context);
    }
}

