using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Helpers;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AuthService(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<User> RegisterUserAsync(RegisterDto dto)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.ToLower(),
            PasswordHash = PasswordHasher.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = "student",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Generate email verification token
        var verificationToken = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
        var emailVerification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database
        _context.Users.Add(user);
        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send verification email (fire and forget - don't wait for it)
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);
            }
            catch
            {
                // Log error but don't fail registration
                // In production, use proper logging
            }
        });

        return user;
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

