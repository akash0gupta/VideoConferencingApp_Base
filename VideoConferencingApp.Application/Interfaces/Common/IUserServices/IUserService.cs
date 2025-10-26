using VideoConferencingApp.Domain.Entities.User;

namespace VideoConferencingApp.Application.Interfaces.Common.IUserServices
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
    }
}
