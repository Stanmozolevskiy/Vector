using Vector.Api.DTOs.User;
using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task<string> UploadProfilePictureAsync(Guid userId, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteProfilePictureAsync(Guid userId);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}

