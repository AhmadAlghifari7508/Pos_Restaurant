using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.AuthViewModels;
using POSRestoran01.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace POSRestoran01.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateAsync(LoginViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user != null && VerifyPassword(model.Password, user.Password))
            {
                // Update last login
                user.LastLogin = DateTime.Now;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return user;
            }

            return null;
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            return user != null && VerifyPassword(password, user.Password);
        }

        // Gunakan BCrypt yang lebih secure
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        // Method untuk create user (hanya untuk admin)
        public async Task<User> CreateUserAsync(string fullName, string username, string email, string password, string role = "Cashier")
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
                throw new InvalidOperationException("Username atau email sudah digunakan");

            var user = new User
            {
                FullName = fullName,
                Username = username,
                Email = email,
                Password = HashPassword(password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // Method untuk change password
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !VerifyPassword(currentPassword, user.Password))
                return false;

            user.Password = HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
    