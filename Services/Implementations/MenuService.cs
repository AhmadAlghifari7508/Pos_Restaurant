using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;

        public MenuService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive) // Only active items
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId)
        {
            // If categoryId is 0, return all menu items
            if (categoryId == 0)
            {
                return await GetAllMenuItemsAsync();
            }

            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.CategoryId == categoryId && m.IsActive)
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetActiveMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive)
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> SearchMenuItemsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllMenuItemsAsync();
            }

            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && (
                    m.ItemName.Contains(searchTerm) ||
                    m.Description.Contains(searchTerm) ||
                    m.Category.CategoryName.Contains(searchTerm)
                ))
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id && m.IsActive);
        }

        public async Task<MenuItem> CreateMenuItemAsync(MenuItem menuItem)
        {
            menuItem.CreatedAt = DateTime.Now;
            menuItem.UpdatedAt = DateTime.Now;
            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        public async Task<MenuItem> UpdateMenuItemAsync(MenuItem menuItem)
        {
            menuItem.UpdatedAt = DateTime.Now;
            _context.MenuItems.Update(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        public async Task<bool> DeleteMenuItemAsync(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                menuItem.IsActive = false;
                menuItem.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateStockAsync(int menuItemId, int quantity)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem != null && menuItem.Stock >= quantity)
            {
                menuItem.Stock -= quantity;
                menuItem.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        // Method tambahan untuk mendukung HomeController
        public async Task<bool> CheckStockAvailabilityAsync(int menuItemId, int requestedQuantity)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            return menuItem != null && menuItem.IsActive && menuItem.Stock >= requestedQuantity;
        }

        public async Task<int> GetAvailableStockAsync(int menuItemId)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            return menuItem?.Stock ?? 0;
        }

        public async Task<List<MenuItem>> GetLowStockItemsAsync(int threshold = 5)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();
        }

        public async Task<bool> IsMenuItemActiveAsync(int menuItemId)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            return menuItem != null && menuItem.IsActive;
        }

        public async Task<List<MenuItem>> GetPopularMenuItemsAsync(int count = 10)
        {
            // Ambil menu items yang paling sering dipesan
            return await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.OrderDetails)
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.OrderDetails.Sum(od => od.Quantity))
                .Take(count)
                .ToListAsync();
        }

        public async Task<decimal> GetMenuItemPriceAsync(int menuItemId)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            return menuItem?.Price ?? 0;
        }
    }
}