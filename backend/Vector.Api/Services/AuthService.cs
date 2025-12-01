using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Helpers;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthService> _logger;
    private readonly IJwtService _jwtService;

    public AuthService(
        ApplicationDbContext context, 
        IEmailService emailService, 
        IServiceProvider serviceProvider, 
        ILogger<AuthService> logger,
        IJwtService jwtService)
    {
        _context = context;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _jwtService = jwtService;
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
                _logger.LogInformation("Attempting to send verification email to {Email}", user.Email);
                await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);
                _logger.LogInformation("Verification email task completed for {Email}", user.Email);
            }
            catch (Exception ex)
            {
                // Log error but don't fail registration
                _logger.LogError(ex, "Failed to send verification email to {Email}. Error: {Message}", user.Email, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            }
        });

        return user;
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Verify password
        if (!PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password attempt for user: {Email}, UserId: {UserId}", dto.Email, user.Id);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Check if email is verified
        if (!user.EmailVerified)
        {
            _logger.LogWarning("Login attempt with unverified email: {Email}, UserId: {UserId}", dto.Email, user.Id);
            throw new InvalidOperationException("Please verify your email address before logging in.");
        }

        // Generate JWT access token
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Role);

        _logger.LogInformation("User logged in successfully: {Email}, UserId: {UserId}", user.Email, user.Id);

        return accessToken;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Email verification attempted with empty token");
            return false;
        }

        // Find the verification record
        var emailVerification = await _context.EmailVerifications
            .Include(ev => ev.User)
            .FirstOrDefaultAsync(ev => ev.Token == token);

        if (emailVerification == null)
        {
            _logger.LogWarning("Email verification token not found: {Token}", token);
            return false;
        }

        // Check if token is already used
        if (emailVerification.IsUsed)
        {
            _logger.LogWarning("Email verification token already used: {Token}, UserId: {UserId}", 
                token, emailVerification.UserId);
            return false;
        }

        // Check if token is expired
        if (emailVerification.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Email verification token expired: {Token}, ExpiresAt: {ExpiresAt}, UserId: {UserId}", 
                token, emailVerification.ExpiresAt, emailVerification.UserId);
            return false;
        }

        // Mark token as used
        emailVerification.IsUsed = true;

        // Update user's email verified status
        var user = emailVerification.User;
        if (user != null)
        {
            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Email verified for user: {Email}, UserId: {UserId}", user.Email, user.Id);
        }

        // Save changes
        await _context.SaveChangesAsync();

        return true;
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

