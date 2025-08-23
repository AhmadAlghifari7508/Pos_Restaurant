// Lokasi: Services/Interfaces/IAuthService.cs
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.AuthViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(LoginViewModel model);
        Task<bool> ValidateUserAsync(string username, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        Task<User> CreateUserAsync(string fullName, string username, string email, string password, string role = "Cashier");
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}