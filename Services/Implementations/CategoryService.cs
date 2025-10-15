using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            category.CreatedAt = DateTime.Now;
            category.UpdatedAt = DateTime.Now;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            category.UpdatedAt = DateTime.Now;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var category = await _context.Categories
                    .Include(c => c.MenuItems)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                    return false;

  
                if (category.MenuItems != null && category.MenuItems.Any())
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"Kategori '{category.CategoryName}' tidak dapat dihapus karena masih memiliki {category.MenuItems.Count} menu item. Hapus menu terlebih dahulu.");
                }


                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync();
                throw; 
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error deleting category: {ex.Message}");
                throw new InvalidOperationException($"Gagal menghapus kategori: {ex.Message}", ex);
            }
        }


        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryId == id && c.IsActive);
        }

        public async Task<int> GetTotalCategoriesAsync()
        {
            return await _context.Categories.CountAsync(c => c.IsActive);
        }

        public async Task<List<Category>> GetCategoriesWithMenuCountAsync()
        {
            return await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
}