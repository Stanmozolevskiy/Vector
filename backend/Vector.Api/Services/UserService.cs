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
    // private readonly IS3Service _s3Service;  // Uncomment when ready to use

    public UserService(ApplicationDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
        // _s3Service = s3Service;  // Uncomment when ready to use
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

        _logger.LogInformation("Updating user {UserId}. Current data: FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}, Location={Location}", 
            userId, user.FirstName, user.LastName, user.PhoneNumber, user.Location);

        // Update only provided fields
        if (dto.FirstName != null)
        {
            user.FirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? null : dto.FirstName.Trim();
        }
        
        if (dto.LastName != null)
        {
            user.LastName = string.IsNullOrWhiteSpace(dto.LastName) ? null : dto.LastName.Trim();
        }
        
        if (dto.Bio != null)
        {
            user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
        }

        // IMPORTANT: Update phone and location even if empty string
        if (dto.PhoneNumber != null)
        {
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            _logger.LogInformation("Setting PhoneNumber to: {PhoneNumber}", user.PhoneNumber);
        }

        if (dto.Location != null)
        {
            user.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            _logger.LogInformation("Setting Location to: {Location}", user.Location);
        }

        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} updated. New data: FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}, Location={Location}", 
            userId, user.FirstName, user.LastName, user.PhoneNumber, user.Location);
        
        return user;
    }

    public async Task<string> UploadProfilePictureAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Delete old profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            // TODO: Uncomment when S3Service is registered
            // await _s3Service.DeleteFileAsync(user.ProfilePictureUrl);
        }

        // Upload new profile picture
        // TODO: Uncomment when S3Service is registered
        // var pictureUrl = await _s3Service.UploadFileAsync(fileStream, fileName, contentType, "profile-pictures");
        
        // For now, throw not implemented
        throw new NotImplementedException("S3Service needs to be registered in Program.cs. See S3_SETUP_GUIDE.md");
        
        // user.ProfilePictureUrl = pictureUrl;
        // user.UpdatedAt = DateTime.UtcNow;
        // await _context.SaveChangesAsync();
        // return pictureUrl;
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

