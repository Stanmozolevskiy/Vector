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
}

