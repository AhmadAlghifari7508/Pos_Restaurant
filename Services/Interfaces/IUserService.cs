using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.SettingsViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(CreateUserViewModel model);
        Task<User> UpdateUserAsync(UpdateUserViewModel model);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ToggleUserStatusAsync(int id);
    }
}
