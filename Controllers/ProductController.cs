using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.ProductViewModels;
using POSRestoran01.Services.Interfaces;
using System.Text.Json;

namespace POSRestoran01.Controllers
{
    public class ProductController : BaseController
    {
        private readonly ICategoryService _categoryService;
        private readonly IMenuService _menuService;
        private readonly IStockHistoryService _stockHistoryService;

        public ProductController(
            ICategoryService categoryService,
            IMenuService menuService,
            IStockHistoryService stockHistoryService)
        {
            _categoryService = categoryService;
            _menuService = menuService;
            _stockHistoryService = stockHistoryService;
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            try
            {
                var model = new ProductManagementViewModel
                {
                    Categories = await _categoryService.GetActiveCategoriesAsync(),
                    SelectedCategoryId = categoryId ?? 0
                };

                // Load menu items based on category
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    model.MenuItems = await _menuService.GetMenuItemsByCategoryAsync(categoryId.Value);
                }
                else if (model.Categories.Any())
                {
                    // Default to first category if no category selected
                    model.SelectedCategoryId = model.Categories.First().CategoryId;
                    model.MenuItems = await _menuService.GetMenuItemsByCategoryAsync(model.SelectedCategoryId);
                }
                else
                {
                    model.MenuItems = new List<MenuItem>();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Terjadi kesalahan saat memuat halaman Product Management.";
                return View(new ProductManagementViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuByCategory(int categoryId)
        {
            try
            {
                var menuItems = await _menuService.GetMenuItemsByCategoryAsync(categoryId);
                return PartialView("_ProductMenuItemsPartial", menuItems);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error in GetMenuByCategory: {ex.Message}");
                return BadRequest("Terjadi kesalahan saat memuat menu");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _categoryService.GetActiveCategoriesAsync();
                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCategories: {ex.Message}");
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat kategori" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return Json(new { success = false, message = "Nama kategori tidak boleh kosong" });
                }

                var category = new Category
                {
                    CategoryName = categoryName.Trim(),
                    IsActive = true
                };

                await _categoryService.CreateCategoryAsync(category);
                return Json(new { success = true, message = "Kategori berhasil ditambahkan", data = category });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateCategory: {ex.Message}");
                return Json(new { success = false, message = "Terjadi kesalahan saat menambahkan kategori" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(int categoryId, string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return Json(new { success = false, message = "Nama kategori tidak boleh kosong" });
                }

                var category = await _categoryService.GetCategoryByIdAsync(categoryId);
                if (category == null)
                {
                    return Json(new { success = false, message = "Kategori tidak ditemukan" });
                }

                category.CategoryName = categoryName.Trim();
                await _categoryService.UpdateCategoryAsync(category);

                return Json(new { success = true, message = "Kategori berhasil diperbarui", data = category });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateCategory: {ex.Message}");
                return Json(new { success = false, message = "Terjadi kesalahan saat memperbarui kategori" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            try
            {
                var success = await _categoryService.DeleteCategoryAsync(categoryId);
                if (success)
                {
                    return Json(new { success = true, message = "Kategori berhasil dihapus" });
                }
                return Json(new { success = false, message = "Kategori tidak ditemukan" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteCategory: {ex.Message}");
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus kategori" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(CreateMenuItemViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                string imagePath = null;
                if (model.ImageFile != null)
                {
                    imagePath = await SaveImageAsync(model.ImageFile);
                }

                var menuItem = new MenuItem
                {
                    CategoryId = model.CategoryId,
                    ItemName = model.ItemName,
                    Description = model.Description,
                    Price = model.Price,
                    Stock = model.Stock,
                    ImagePath = imagePath,
                    IsActive = model.IsActive, // Fix: Ensure this properly maps the checkbox value
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _menuService.CreateMenuItemAsync(menuItem);

                // Record stock history
                await _stockHistoryService.RecordStockChangeAsync(
                    menuItem.MenuItemId,
                    GetCurrentUserId(),
                    0,
                    menuItem.Stock,
                    "Initial Stock",
                    $"Initial stock for new menu item: {menuItem.ItemName}"
                );

                return Json(new { success = true, message = "Menu item berhasil ditambahkan" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateMenuItem: {ex.Message}");
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItem(int menuItemId)
        {
            try
            {
                var menuItem = await _menuService.GetMenuItemByIdAsync(menuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item tidak ditemukan" });
                }

                // Return proper JSON structure with all needed fields
                var result = new
                {
                    success = true,
                    data = new
                    {
                        menuItemId = menuItem.MenuItemId,
                        categoryId = menuItem.CategoryId,
                        itemName = menuItem.ItemName,
                        description = menuItem.Description ?? "",
                        price = menuItem.Price,
                        stock = menuItem.Stock,
                        imagePath = menuItem.ImagePath ?? "",
                        isActive = menuItem.IsActive
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMenuItem: {ex.Message}");
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat menu item" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMenuItem(UpdateMenuItemViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var menuItem = await _menuService.GetMenuItemByIdAsync(model.MenuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item tidak ditemukan" });
                }

                var oldStock = menuItem.Stock;
                var stockChanged = oldStock != model.Stock;

                // Update menu item
                menuItem.CategoryId = model.CategoryId;
                menuItem.ItemName = model.ItemName;
                menuItem.Description = model.Description;
                menuItem.Price = model.Price;
                menuItem.Stock = model.Stock;
                menuItem.IsActive = model.IsActive;
                menuItem.UpdatedAt = DateTime.Now;

                // Handle image update
                if (model.ImageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(menuItem.ImagePath))
                    {
                        DeleteImage(menuItem.ImagePath);
                    }
                    menuItem.ImagePath = await SaveImageAsync(model.ImageFile);
                }

                await _menuService.UpdateMenuItemAsync(menuItem);

                // Record stock history if stock changed
                if (stockChanged)
                {
                    await _stockHistoryService.RecordStockChangeAsync(
                        menuItem.MenuItemId,
                        GetCurrentUserId(),
                        oldStock,
                        model.Stock,
                        "Manual Update",
                        $"Stock updated for {menuItem.ItemName} from {oldStock} to {model.Stock}"
                    );
                }

                return Json(new { success = true, message = "Menu item berhasil diperbarui" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateMenuItem: {ex.Message}");
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItem(int menuItemId)
        {
            try
            {
                var success = await _menuService.DeleteMenuItemAsync(menuItemId);
                if (success)
                {
                    return Json(new { success = true, message = "Menu item berhasil dihapus" });
                }
                return Json(new { success = false, message = "Menu item tidak ditemukan" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteMenuItem: {ex.Message}");
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus menu item" });
            }
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "menu");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/menu/{uniqueFileName}";
        }

        private void DeleteImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}