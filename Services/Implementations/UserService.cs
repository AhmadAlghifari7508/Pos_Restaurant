using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.SettingsViewModels;
using POSRestoran01.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace POSRestoran01.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(CreateUserViewModel model)
        {
            var user = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                Password = HashPassword(model.Password),
                Role = model.Role,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(UpdateUserViewModel model)
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
                throw new ArgumentException("User not found");

            user.FullName = model.FullName;
            user.Username = model.Username;
            user.Email = model.Email;
            user.Role = model.Role;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.Now;

            // Update password if provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.Password = HashPassword(model.NewPassword);
            }

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ToggleUserStatusAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}