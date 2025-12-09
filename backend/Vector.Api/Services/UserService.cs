using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.User;
using Vector.Api.Helpers;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IS3Service _s3Service;

    public UserService(ApplicationDbContext context, ILogger<UserService> logger, IS3Service s3Service)
    {
        _context = context;
        _logger = logger;
        _s3Service = s3Service;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            return null;
        }

        _logger.LogInformation("Updating user {UserId}. Current data: FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}, Location={Location}", 
            userId, user.FirstName, user.LastName, user.PhoneNumber, user.Location);
        
        _logger.LogInformation("Received DTO: FirstName={FirstName}, LastName={LastName}, Bio={Bio}, PhoneNumber={PhoneNumber}, Location={Location}",
            dto.FirstName, dto.LastName, dto.Bio, dto.PhoneNumber, dto.Location);

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
        {
            user.FirstName = dto.FirstName.Trim();
        }
        
        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            user.LastName = dto.LastName.Trim();
        }
        
        if (!string.IsNullOrWhiteSpace(dto.Bio))
        {
            user.Bio = dto.Bio.Trim();
        }

        // IMPORTANT: Always update phone and location (allow clearing)
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        _logger.LogInformation("Setting PhoneNumber to: {PhoneNumber}", user.PhoneNumber ?? "NULL");

        user.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
        _logger.LogInformation("Setting Location to: {Location}", user.Location ?? "NULL");

        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} updated successfully. New data: PhoneNumber={PhoneNumber}, Location={Location}", 
            userId, user.PhoneNumber ?? "NULL", user.Location ?? "NULL");
        
        _logger.LogInformation("User {UserId} updated. New data: FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}, Location={Location}", 
            userId, user.FirstName, user.LastName, user.PhoneNumber, user.Location);
        
        return user;
    }

    public async Task<string> UploadProfilePictureAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        // Optimize: Use AsNoTracking for initial check, then track for update
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Delete old profile picture if exists (fire and forget to avoid blocking)
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _s3Service.DeleteFileAsync(user.ProfilePictureUrl);
                    _logger.LogInformation("Deleted old profile picture for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old profile picture for user {UserId}", userId);
                    // Continue with upload even if delete fails
                }
            });
        }

        // Upload new profile picture
        var pictureUrl = await _s3Service.UploadFileAsync(fileStream, fileName, contentType, "profile-pictures");
        
        // Re-attach user for update
        user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.ProfilePictureUrl = pictureUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        
        _logger.LogInformation("Profile picture uploaded successfully for user {UserId}: {Url}", userId, pictureUrl);
        return pictureUrl;
    }

    public async Task<bool> DeleteProfilePictureAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null || string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            return false;
        }

        try
        {
            await _s3Service.DeleteFileAsync(user.ProfilePictureUrl);
            user.ProfilePictureUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Profile picture deleted successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile picture for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            return false;
        }

        // Verify current password
        if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            return false;
        }

        // Update password
        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Delete user (cascade will handle related data)
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted successfully", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return false;
        }
    }
}

