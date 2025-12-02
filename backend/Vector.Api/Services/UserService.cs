using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.User;
using Vector.Api.Helpers;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    // TODO: Add IS3Service dependency

    public UserService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<User> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

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

        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return user;
    }

    public Task<string> UploadProfilePictureAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        // TODO: Implement profile picture upload with S3
        // Will be implemented when S3Service is ready
        throw new NotImplementedException("Profile picture upload will be implemented with S3 integration");
    }

    public Task<bool> DeleteProfilePictureAsync(Guid userId)
    {
        // TODO: Implement delete profile picture from S3
        throw new NotImplementedException("Profile picture deletion will be implemented with S3 integration");
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify current password
        if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Update password
        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return true;
    }
}

