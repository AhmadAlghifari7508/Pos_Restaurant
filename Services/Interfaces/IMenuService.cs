using POSRestoran01.Models;

namespace POSRestoran01.Services.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuItem>> GetAllMenuItemsAsync();
        Task<List<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId);
        Task<List<MenuItem>> GetActiveMenuItemsAsync();
        Task<List<MenuItem>> SearchMenuItemsAsync(string searchTerm);
        Task<MenuItem?> GetMenuItemByIdAsync(int id);
        Task<MenuItem> CreateMenuItemAsync(MenuItem menuItem);
        Task<MenuItem> UpdateMenuItemAsync(MenuItem menuItem);
        Task<bool> DeleteMenuItemAsync(int id);
        Task<bool> UpdateStockAsync(int menuItemId, int quantity);

        // Method tambahan
        Task<bool> CheckStockAvailabilityAsync(int menuItemId, int requestedQuantity);
        Task<int> GetAvailableStockAsync(int menuItemId);
        Task<List<MenuItem>> GetLowStockItemsAsync(int threshold = 5);
        Task<bool> IsMenuItemActiveAsync(int menuItemId);
        Task<List<MenuItem>> GetPopularMenuItemsAsync(int count = 10);
        Task<decimal> GetMenuItemPriceAsync(int menuItemId);
    }
}