using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    // TODO: Add other dependencies (IJwtService, IEmailService, etc.)

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User> RegisterUserAsync(RegisterDto dto)
    {
        // TODO: Implement user registration
        throw new NotImplementedException();
    }

    public Task<string> LoginAsync(LoginDto dto)
    {
        // TODO: Implement login
        throw new NotImplementedException();
    }

    public Task<bool> VerifyEmailAsync(string token)
    {
        // TODO: Implement email verification
        throw new NotImplementedException();
    }

    public Task<bool> ForgotPasswordAsync(string email)
    {
        // TODO: Implement forgot password
        throw new NotImplementedException();
    }

    public Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        // TODO: Implement reset password
        throw new NotImplementedException();
    }

    public Task<bool> LogoutAsync(Guid userId)
    {
        // TODO: Implement logout
        throw new NotImplementedException();
    }

    public Task<string> RefreshTokenAsync(string refreshToken)
    {
        // TODO: Implement refresh token
        throw new NotImplementedException();
    }
}

