using Microsoft.AspNetCore.Http;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.UserDto;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.UserServices
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(User user);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(long id);
        Task<IList<User>> GetAllAsync();
        Task<bool> DeactivateUserAsync(long id, string reason = null);
        Task<bool> SoftDeleteUserAsync(long id, long? deletedBy = null);
        Task<User> UpdateUserAsync(User user);

        Task<UserProfileDto> GetUserProfileAsync(long userId, long requesterId);
        Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileDto dto);
        Task<string> UpdateProfilePictureAsync(long userId, IFormFile file);
        Task<bool> DeleteProfilePictureAsync(long userId);
        Task<bool> DeleteAccountAsync(long userId, string password, string reason = null);
        Task<UserSecuritySettingsDto> GetSecuritySettingsAsync(long userId);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
    }
}
