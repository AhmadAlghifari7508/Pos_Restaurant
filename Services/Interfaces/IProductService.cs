using POSRestoran01.Models;

namespace POSRestoran01.Services.Interfaces
{
    public interface IProductService
    {
        // MenuItem CRUD Operations
        Task<List<MenuItem>> GetAllMenuItemsAsync();
        Task<List<MenuItem>> GetActiveMenuItemsAsync();
        Task<List<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId);
        Task<MenuItem?> GetMenuItemByIdAsync(int id);
        Task<MenuItem?> CreateMenuItemAsync(MenuItem menuItem);
        Task<bool> UpdateMenuItemAsync(MenuItem menuItem);
        Task<bool> DeleteMenuItemAsync(int id);
        Task<bool> UpdateMenuItemStockAsync(int menuItemId, int newStock);

        // Stock History Operations
        Task<List<StockHistory>> GetStockHistoryAsync();
        Task<List<StockHistory>> GetStockHistoryByMenuItemAsync(int menuItemId);
        Task<List<StockHistory>> GetStockHistoryByUserAsync(int userId);
        Task<bool> LogStockHistoryAsync(int menuItemId, int userId, int previousStock, int newStock, string changeType, string? notes = null);

        // Search and Filter Operations
        Task<List<MenuItem>> SearchMenuItemsAsync(string searchTerm);
        Task<List<MenuItem>> GetMenuItemsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<List<MenuItem>> GetLowStockMenuItemsAsync(int threshold = 10);
    }
}