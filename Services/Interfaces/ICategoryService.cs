using POSRestoran01.Models;

namespace POSRestoran01.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<Category>> GetActiveCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);

        // Method tambahan
        Task<bool> CategoryExistsAsync(int id);
        Task<int> GetTotalCategoriesAsync();
        Task<List<Category>> GetCategoriesWithMenuCountAsync();
    }
}