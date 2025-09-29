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

        // Method untuk logout
        Task LogoutAsync(int userId);

        // TAMBAH: Method untuk mendapatkan user by ID
        Task<User?> GetUserByIdAsync(int userId);

        // TAMBAH: Method untuk update user profile
        Task<bool> UpdateUserProfileAsync(int userId, string fullName, string email, string? newPassword = null);

        // TAMBAH: Method untuk validasi password lama
        Task<bool> ValidateCurrentPasswordAsync(int userId, string currentPassword);
    }
}