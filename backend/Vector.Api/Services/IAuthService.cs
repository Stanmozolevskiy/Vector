using Vector.Api.DTOs.Auth;
using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IAuthService
{
    Task<User> RegisterUserAsync(RegisterDto dto);
    Task<string> LoginAsync(LoginDto dto);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ResendVerificationEmailAsync(string email);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> LogoutAsync(Guid userId);
    Task<string> RefreshTokenAsync(string refreshToken);
    Task<string> GetLatestRefreshTokenAsync(string oldRefreshToken);
}

