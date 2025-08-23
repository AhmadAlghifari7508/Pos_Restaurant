using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        // MenuItem CRUD Operations
        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetActiveMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.Category.IsActive)
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.CategoryId == categoryId && m.IsActive)
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);
        }

        public async Task<MenuItem?> CreateMenuItemAsync(MenuItem menuItem)
        {
            try
            {
                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();

                return await GetMenuItemByIdAsync(menuItem.MenuItemId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateMenuItemAsync(MenuItem menuItem)
        {
            try
            {
                _context.MenuItems.Update(menuItem);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteMenuItemAsync(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem != null)
                {
                    _context.MenuItems.Remove(menuItem);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateMenuItemStockAsync(int menuItemId, int newStock)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(menuItemId);
                if (menuItem != null)
                {
                    menuItem.Stock = newStock;
                    menuItem.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Stock History Operations
        public async Task<List<StockHistory>> GetStockHistoryAsync()
        {
            return await _context.StockHistories
                .Include(s => s.MenuItem)
                .Include(s => s.User)
                .OrderByDescending(s => s.ChangedAt)
                .ToListAsync();
        }

        public async Task<List<StockHistory>> GetStockHistoryByMenuItemAsync(int menuItemId)
        {
            return await _context.StockHistories
                .Include(s => s.MenuItem)
                .Include(s => s.User)
                .Where(s => s.MenuItemId == menuItemId)
                .OrderByDescending(s => s.ChangedAt)
                .ToListAsync();
        }

        public async Task<List<StockHistory>> GetStockHistoryByUserAsync(int userId)
        {
            return await _context.StockHistories
                .Include(s => s.MenuItem)
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> LogStockHistoryAsync(int menuItemId, int userId, int previousStock, int newStock, string changeType, string? notes = null)
        {
            try
            {
                var stockHistory = new StockHistory
                {
                    MenuItemId = menuItemId,
                    UserId = userId,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    StockChange = newStock - previousStock,
                    ChangeType = changeType,
                    Notes = notes,
                    ChangedAt = DateTime.Now
                };

                _context.StockHistories.Add(stockHistory);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Search and Filter Operations
        public async Task<List<MenuItem>> SearchMenuItemsAsync(string searchTerm)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.Category.IsActive &&
                           (m.ItemName.Contains(searchTerm) ||
                            m.Description != null && m.Description.Contains(searchTerm) ||
                            m.Category.CategoryName.Contains(searchTerm)))
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetMenuItemsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.Category.IsActive &&
                           m.Price >= minPrice && m.Price <= maxPrice)
                .OrderBy(m => m.Price)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetLowStockMenuItemsAsync(int threshold = 10)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.Category.IsActive && m.Stock <= threshold)
                .OrderBy(m => m.Stock)
                .ToListAsync();
        }
    }
}