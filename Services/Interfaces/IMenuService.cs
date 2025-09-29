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

        // Method tambahan untuk stock management
        Task<bool> CheckStockAvailabilityAsync(int menuItemId, int requestedQuantity);
        Task<int> GetAvailableStockAsync(int menuItemId);
        Task<List<MenuItem>> GetLowStockItemsAsync(int threshold = 5);
        Task<bool> IsMenuItemActiveAsync(int menuItemId);
        Task<List<MenuItem>> GetPopularMenuItemsAsync(int count = 10);
        Task<decimal> GetMenuItemPriceAsync(int menuItemId);

        // New methods for discount support
        Task<List<MenuItem>> GetMenuItemsWithActiveDiscountAsync();
        Task<List<MenuItem>> GetMenuItemsWithDiscountByCategoryAsync(int categoryId);
        Task<decimal> GetDiscountedPriceAsync(int menuItemId, DateTime? checkDate = null);
        Task<bool> IsDiscountValidAsync(int menuItemId, DateTime? checkDate = null);
        Task<List<MenuItem>> GetMenuItemsByDiscountPercentageAsync(decimal minPercentage, decimal maxPercentage);
        Task<Dictionary<string, object>> GetDiscountStatisticsAsync();
        Task<bool> UpdateDiscountStatusAsync(int menuItemId, bool isActive);
        Task<List<MenuItem>> GetExpiringDiscountsAsync(int daysFromNow = 7);
        Task<bool> IncreaseStockAsync(int menuItemId, int quantity, int userId, string? notes = null);
    }
}