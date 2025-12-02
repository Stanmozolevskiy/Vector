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
        // Using Task.Run with proper error handling
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogWarning("=== STARTING EMAIL SEND TASK ===");
                _logger.LogWarning("Attempting to send verification email to {Email}", user.Email);
                _logger.LogWarning("Verification token: {Token}", verificationToken);
                
                await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);
                
                _logger.LogWarning("Verification email task completed successfully for {Email}", user.Email);
                _logger.LogWarning("=== EMAIL SEND TASK COMPLETED ===");
            }
            catch (Exception ex)
            {
                // Log error but don't fail registration
                _logger.LogError(ex, "=== EMAIL SEND TASK FAILED ===");
                _logger.LogError("Failed to send verification email to {Email}. Error: {Message}", user.Email, ex.Message);
                _logger.LogError("Exception type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                _logger.LogError("=== END EMAIL SEND TASK ERROR ===");
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

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Forgot password attempted with empty email");
            return false;
        }

        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        // Always return true to prevent email enumeration
        // But only send email if user exists
        if (user == null)
        {
            _logger.LogWarning("Forgot password attempted for non-existent email: {Email}", email);
            return true; // Return true to prevent email enumeration
        }

        // Generate password reset token
        var resetToken = TokenGenerator.GeneratePasswordResetToken();
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Invalidate any existing unused reset tokens for this user
        var existingResets = await _context.PasswordResets
            .Where(pr => pr.UserId == user.Id && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var existing in existingResets)
        {
            existing.IsUsed = true;
        }

        // Save new reset token
        _context.PasswordResets.Add(passwordReset);
        await _context.SaveChangesAsync();

        // Send password reset email (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Attempting to send password reset email to {Email}", user.Email);
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
                _logger.LogInformation("Password reset email sent successfully to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}. Error: {Message}", user.Email, ex.Message);
            }
        });

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            _logger.LogWarning("Reset password attempted with invalid data");
            return false;
        }

        // Find the password reset record
        var passwordReset = await _context.PasswordResets
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr => pr.Token == dto.Token);

        if (passwordReset == null)
        {
            _logger.LogWarning("Password reset token not found: {Token}", dto.Token);
            return false;
        }

        // Verify email matches
        if (passwordReset.User.Email.ToLower() != dto.Email.ToLower())
        {
            _logger.LogWarning("Password reset email mismatch. Token: {Token}, Expected: {ExpectedEmail}, Provided: {ProvidedEmail}", 
                dto.Token, passwordReset.User.Email, dto.Email);
            return false;
        }

        // Check if token is already used
        if (passwordReset.IsUsed)
        {
            _logger.LogWarning("Password reset token already used: {Token}, UserId: {UserId}", 
                dto.Token, passwordReset.UserId);
            return false;
        }

        // Check if token is expired
        if (passwordReset.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset token expired: {Token}, ExpiresAt: {ExpiresAt}, UserId: {UserId}", 
                dto.Token, passwordReset.ExpiresAt, passwordReset.UserId);
            return false;
        }

        // Update user password
        var user = passwordReset.User;
        user.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Mark token as used
        passwordReset.IsUsed = true;

        // Save changes
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successful for user: {Email}, UserId: {UserId}", user.Email, user.Id);

        return true;
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

