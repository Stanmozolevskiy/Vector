using System.Security.Claims;

namespace Vector.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string role);
    string GenerateRefreshToken(Guid userId);
    ClaimsPrincipal? ValidateToken(string token);
    ClaimsPrincipal? ValidateRefreshToken(string token);
    Guid? GetUserIdFromToken(string token);
}

