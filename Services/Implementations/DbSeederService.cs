using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public interface IDbSeederService
    {
        Task SeedInitialDataAsync();
        Task<bool> HasInitialDataAsync();
    }

    public class DbSeederService : IDbSeederService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public DbSeederService(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<bool> HasInitialDataAsync()
        {
            return await _context.Users.AnyAsync() ||
                   await _context.Categories.AnyAsync();
        }

        public async Task SeedInitialDataAsync()
        {
            // Seed Users
            if (!await _context.Users.AnyAsync())
            {
                var defaultUsers = new[]
                {
                    new { FullName = "Kasir 1", Username = "kasir1", Email = "kasir1@resto.com", Password = "kasir123", Role = "Cashier" },
                    new { FullName = "Kasir 2", Username = "kasir2", Email = "kasir2@resto.com", Password = "kasir123", Role = "Cashier" }
                };

                foreach (var userData in defaultUsers)
                {
                    var user = new User
                    {
                        FullName = userData.FullName,
                        Username = userData.Username,
                        Email = userData.Email,
                        Password = _authService.HashPassword(userData.Password),
                        Role = userData.Role,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                }

                await _context.SaveChangesAsync();
            }

            // Seed Categories
            if (!await _context.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category { CategoryName = "Makanan", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new Category { CategoryName = "Minuman", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new Category { CategoryName = "Snack", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new Category { CategoryName = "Dessert", IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };

                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();
            }

            // Seed Sample Menu Items
            if (!await _context.MenuItems.AnyAsync())
            {
                var makananCategory = await _context.Categories.FirstAsync(c => c.CategoryName == "Makanan");
                var minumanCategory = await _context.Categories.FirstAsync(c => c.CategoryName == "Minuman");

                var menuItems = new[]
                {
                    new MenuItem { CategoryId = makananCategory.CategoryId, ItemName = "Nasi Goreng", Description = "Nasi goreng spesial dengan telur", Price = 25000, Stock = 50, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new MenuItem { CategoryId = makananCategory.CategoryId, ItemName = "Mie Ayam", Description = "Mie ayam dengan bakso", Price = 20000, Stock = 30, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new MenuItem { CategoryId = makananCategory.CategoryId, ItemName = "Ayam Bakar", Description = "Ayam bakar bumbu kecap", Price = 35000, Stock = 20, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new MenuItem { CategoryId = minumanCategory.CategoryId, ItemName = "Es Teh", Description = "Es teh manis segar", Price = 8000, Stock = 100, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new MenuItem { CategoryId = minumanCategory.CategoryId, ItemName = "Es Jeruk", Description = "Es jeruk peras asli", Price = 12000, Stock = 50, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new MenuItem { CategoryId = minumanCategory.CategoryId, ItemName = "Kopi Hitam", Description = "Kopi hitam original", Price = 15000, Stock = 40, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };

                _context.MenuItems.AddRange(menuItems);
                await _context.SaveChangesAsync();
            }
        }
    }
}