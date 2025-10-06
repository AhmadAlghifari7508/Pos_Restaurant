using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockHistoryService _stockHistoryService;

        public MenuService(ApplicationDbContext context, IStockHistoryService stockHistoryService)
        {
            _context = context;
            _stockHistoryService = stockHistoryService;
        }

        // ============ UPDATED METHODS FOR PRODUCT MANAGEMENT ============

        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            // For PRODUCT MANAGEMENT - Show ALL menus (Active & Inactive)
            return await _context.MenuItems
                .Include(m => m.Category)
                // NO IsActive filter - shows everything
                .OrderByDescending(m => m.IsActive) // Active items first
                .ThenBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId)
        {
            if (categoryId == 0)
            {
                return await GetAllMenuItemsAsync();
            }

            // For PRODUCT MANAGEMENT - Show ALL menus in category
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.CategoryId == categoryId) // NO IsActive filter
                .OrderByDescending(m => m.IsActive) // Active items first
                .ThenBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetActiveMenuItemsAsync()
        {
            // For HOME/POS PAGE - Only show active items
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
                return await GetActiveMenuItemsAsync(); // For HOME - only active
            }

            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && ( // For HOME - only active
                    m.ItemName.Contains(searchTerm) ||
                    m.Description.Contains(searchTerm) ||
                    m.Category.CategoryName.Contains(searchTerm)
                ))
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            // Get any menu item by ID (for editing), regardless of IsActive status
            return await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);
        }

        public async Task<MenuItem> CreateMenuItemAsync(MenuItem menuItem)
        {
            menuItem.CreatedAt = DateTime.Now;
            menuItem.UpdatedAt = DateTime.Now;

            if (!menuItem.DiscountPercentage.HasValue)
                menuItem.DiscountPercentage = 0.00m;

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        public async Task<MenuItem> UpdateMenuItemAsync(MenuItem menuItem)
        {
            menuItem.UpdatedAt = DateTime.Now;

            if (menuItem.IsDiscountActive && menuItem.DiscountPercentage.HasValue && menuItem.DiscountPercentage > 0)
            {
                if (menuItem.DiscountStartDate.HasValue && menuItem.DiscountEndDate.HasValue)
                {
                    if (menuItem.DiscountEndDate < menuItem.DiscountStartDate)
                    {
                        throw new InvalidOperationException("Tanggal berakhir diskon harus setelah tanggal mulai");
                    }
                }
            }

            _context.MenuItems.Update(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        public async Task<bool> DeleteMenuItemAsync(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                // HARD DELETE - Permanently remove from database
                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        // ============ OTHER METHODS (Unchanged) ============

        public async Task<bool> UpdateStockAsync(int menuItemId, int quantity)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem != null && menuItem.Stock >= quantity)
            {
                var previousStock = menuItem.Stock;
                var newStock = previousStock - quantity;

                await _stockHistoryService.RecordStockChangeAsync(
                    menuItemId,
                    1,
                    previousStock,
                    newStock,
                    "Manual Update",
                    $"Manual stock update - reduced by {quantity}"
                );

                menuItem.Stock = newStock;
                menuItem.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> IncreaseStockAsync(int menuItemId, int quantity, int userId, string? notes = null)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem != null)
            {
                var previousStock = menuItem.Stock;
                var newStock = previousStock + quantity;

                await _stockHistoryService.RecordStockChangeAsync(
                    menuItemId,
                    userId,
                    previousStock,
                    newStock,
                    "Manual Update",
                    notes ?? $"Manual stock increase by {quantity}"
                );

                menuItem.Stock = newStock;
                menuItem.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

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
            return menuItem?.FinalPrice ?? 0;
        }

        public async Task<List<MenuItem>> GetMenuItemsWithActiveDiscountAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.HasActiveDiscount)
                .OrderBy(m => m.ItemName)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetMenuItemsWithDiscountByCategoryAsync(int categoryId)
        {
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.HasActiveDiscount);

            if (categoryId > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId);
            }

            return await query.OrderBy(m => m.ItemName).ToListAsync();
        }

        public async Task<decimal> GetDiscountedPriceAsync(int menuItemId, DateTime? checkDate = null)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            return menuItem?.GetPriceForDate(checkDate) ?? 0;
        }

        public async Task<bool> IsDiscountValidAsync(int menuItemId, DateTime? checkDate = null)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            return menuItem?.IsDiscountValidForDate(checkDate) ?? false;
        }

        public async Task<List<MenuItem>> GetMenuItemsByDiscountPercentageAsync(decimal minPercentage, decimal maxPercentage)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive &&
                           m.IsDiscountActive &&
                           m.DiscountPercentage >= minPercentage &&
                           m.DiscountPercentage <= maxPercentage)
                .OrderByDescending(m => m.DiscountPercentage)
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetDiscountStatisticsAsync()
        {
            var totalMenus = await _context.MenuItems.CountAsync(m => m.IsActive);
            var menusWithActiveDiscount = await _context.MenuItems.CountAsync(m => m.IsActive && m.HasActiveDiscount);
            var averageDiscountPercentage = await _context.MenuItems
                .Where(m => m.IsActive && m.HasActiveDiscount)
                .AverageAsync(m => m.DiscountPercentage ?? 0);

            return new Dictionary<string, object>
            {
                { "TotalActiveMenus", totalMenus },
                { "MenusWithActiveDiscount", menusWithActiveDiscount },
                { "DiscountCoveragePercentage", totalMenus > 0 ? (decimal)menusWithActiveDiscount / totalMenus * 100 : 0 },
                { "AverageDiscountPercentage", Math.Round(averageDiscountPercentage, 2) }
            };
        }

        public async Task<bool> UpdateDiscountStatusAsync(int menuItemId, bool isActive)
        {
            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem != null)
            {
                menuItem.IsDiscountActive = isActive;
                menuItem.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<MenuItem>> GetExpiringDiscountsAsync(int daysFromNow = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(daysFromNow);

            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsActive &&
                           m.IsDiscountActive &&
                           m.DiscountEndDate.HasValue &&
                           m.DiscountEndDate <= cutoffDate)
                .OrderBy(m => m.DiscountEndDate)
                .ToListAsync();
        }
    }
}