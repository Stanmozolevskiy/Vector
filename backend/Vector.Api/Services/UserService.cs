using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.User;
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

    public Task<User> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        // TODO: Implement update profile
        throw new NotImplementedException();
    }

    public Task<string> UploadProfilePictureAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        // TODO: Implement profile picture upload
        throw new NotImplementedException();
    }

    public Task<bool> DeleteProfilePictureAsync(Guid userId)
    {
        // TODO: Implement delete profile picture
        throw new NotImplementedException();
    }

    public Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        // TODO: Implement change password
        throw new NotImplementedException();
    }
}

