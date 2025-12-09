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
    private readonly IRedisService _redisService;

    public AuthService(
        ApplicationDbContext context, 
        IEmailService emailService, 
        IServiceProvider serviceProvider, 
        ILogger<AuthService> logger,
        IJwtService jwtService,
        IRedisService redisService)
    {
        _context = context;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _jwtService = jwtService;
        _redisService = redisService;
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

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginDto dto)
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

        // Generate JWT access token and refresh token
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Role);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

        // Store refresh token in database (for persistence)
        var refreshTokenRecord = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshTokenRecord);
        await _context.SaveChangesAsync();

        // Store refresh token in Redis (for fast access)
        var tokenExpiration = TimeSpan.FromDays(7);
        await _redisService.StoreRefreshTokenAsync(user.Id, refreshToken, tokenExpiration);

        _logger.LogInformation("User logged in successfully: {Email}, UserId: {UserId}. Refresh token stored in Redis and PostgreSQL", user.Email, user.Id);

        return (accessToken, refreshToken);
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

    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Resend verification attempted with empty email");
            return false;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
        {
            _logger.LogWarning("Resend verification attempted for non-existent email: {Email}", email);
            return false; // Don't reveal if email exists
        }

        if (user.EmailVerified)
        {
            _logger.LogWarning("Resend verification attempted for already verified email: {Email}", email);
            return false; // Already verified
        }

        // Invalidate existing verification tokens
        var existingVerifications = await _context.EmailVerifications
            .Where(ev => ev.UserId == user.Id && !ev.IsUsed && ev.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var existing in existingVerifications)
        {
            existing.IsUsed = true;
        }

        // Generate new verification token
        var verificationToken = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
        var emailVerification = new EmailVerification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send verification email (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Resending verification email to {Email}", user.Email);
                await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);
                _logger.LogInformation("Verification email resent successfully to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
            }
        });

        return true;
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        try
        {
            // Revoke all active refresh tokens for this user in database
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                
                // Blacklist token in Redis (fast revocation check)
                var ttl = token.ExpiresAt - DateTime.UtcNow;
                if (ttl.TotalSeconds > 0)
                {
                    await _redisService.BlacklistTokenAsync(token.Token, ttl);
                }
            }

            await _context.SaveChangesAsync();

            // Remove refresh token from Redis
            await _redisService.RevokeRefreshTokenAsync(userId);

            _logger.LogInformation("User {UserId} logged out successfully. Revoked {TokenCount} refresh token(s) in Redis and PostgreSQL", 
                userId, activeTokens.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout user {UserId}", userId);
            throw;
        }
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // 1. Check if token is blacklisted in Redis (FAST CHECK)
            var isBlacklisted = await _redisService.IsTokenBlacklistedAsync(refreshToken);
            if (isBlacklisted)
            {
                _logger.LogWarning("Refresh token is blacklisted");
                throw new UnauthorizedAccessException("Refresh token has been revoked");
            }

            // 2. Validate and decode the refresh token
            var tokenClaims = _jwtService.ValidateRefreshToken(refreshToken);
            if (tokenClaims == null)
            {
                _logger.LogWarning("Invalid refresh token provided");
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var userIdClaim = tokenClaims.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid user ID in refresh token");
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // 3. Check Redis for refresh token (FAST)
            var cachedToken = await _redisService.GetRefreshTokenAsync(userId);
            if (cachedToken != null && cachedToken != refreshToken)
            {
                _logger.LogWarning("Refresh token mismatch in Redis for user {UserId}", userId);
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // 4. Verify in database (persistence layer)
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found or already revoked for user {UserId}", userId);
                throw new UnauthorizedAccessException("Invalid or revoked refresh token");
            }

            // 5. Check if token is expired
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user {UserId}", userId);
                throw new UnauthorizedAccessException("Refresh token has expired");
            }

            // 6. Verify user still exists and is active
            var user = storedToken.User;
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token");
                throw new UnauthorizedAccessException("User not found");
            }

            // 7. ROTATION: Revoke the old refresh token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // 8. Blacklist old token in Redis
            var oldTokenTtl = storedToken.ExpiresAt - DateTime.UtcNow;
            if (oldTokenTtl.TotalSeconds > 0)
            {
                await _redisService.BlacklistTokenAsync(refreshToken, oldTokenTtl);
            }

            // 9. Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Role);
            var newRefreshToken = _jwtService.GenerateRefreshToken(user.Id);

            // 10. Store new refresh token in database
            var newRefreshTokenRecord = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _context.RefreshTokens.Add(newRefreshTokenRecord);
            await _context.SaveChangesAsync();

            // 11. Store new refresh token in Redis (for fast access)
            await _redisService.StoreRefreshTokenAsync(user.Id, newRefreshToken, TimeSpan.FromDays(7));

            _logger.LogInformation("Refresh token rotated successfully for user {UserId}. Token stored in Redis and PostgreSQL", userId);

            return (newAccessToken, newRefreshToken);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw auth exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token");
            throw new UnauthorizedAccessException("Failed to refresh token", ex);
        }
    }

    public async Task<string> GetLatestRefreshTokenAsync(string oldRefreshToken)
    {
        try
        {
            // Validate old token
            var tokenClaims = _jwtService.ValidateRefreshToken(oldRefreshToken);
            if (tokenClaims == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var userIdClaim = tokenClaims.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }

            // Get the most recent non-revoked refresh token for this user
            var latestToken = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .Select(rt => rt.Token)
                .FirstOrDefaultAsync();

            if (latestToken == null)
            {
                _logger.LogWarning("No valid refresh token found for user {UserId}", userId);
                throw new UnauthorizedAccessException("No valid refresh token found");
            }

            return latestToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest refresh token");
            throw new UnauthorizedAccessException("Failed to get latest refresh token", ex);
        }
    }
}


