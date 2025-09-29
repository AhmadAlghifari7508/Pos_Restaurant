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
                    SelectedCategoryId = categoryId ?? 0  // Default to 0 (Semua Kategori)
                };

                // Load menu items based on category
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    model.MenuItems = await _menuService.GetMenuItemsByCategoryAsync(categoryId.Value);
                }
                else
                {
                    // Show all menu items when "Semua Kategori" is selected (categoryId = 0)
                    model.MenuItems = await _menuService.GetAllMenuItemsAsync();
                    model.SelectedCategoryId = 0;
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
                var menuItems = categoryId == 0
                    ? await _menuService.GetAllMenuItemsAsync()
                    : await _menuService.GetMenuItemsByCategoryAsync(categoryId);

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

                // Enhanced discount validation
                if (model.IsDiscountActive)
                {
                    if (!model.DiscountPercentage.HasValue || model.DiscountPercentage <= 0)
                    {
                        return Json(new { success = false, message = "Persentase diskon harus lebih dari 0 jika diskon aktif" });
                    }

                    if (model.DiscountPercentage > 100)
                    {
                        return Json(new { success = false, message = "Persentase diskon tidak boleh lebih dari 100%" });
                    }

                    if (model.DiscountStartDate.HasValue && model.DiscountEndDate.HasValue)
                    {
                        if (model.DiscountEndDate < model.DiscountStartDate)
                        {
                            return Json(new { success = false, message = "Tanggal berakhir diskon harus setelah tanggal mulai" });
                        }
                    }
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
                    IsActive = model.IsActive,
                    // Enhanced discount properties
                    DiscountPercentage = model.IsDiscountActive ? (model.DiscountPercentage ?? 0) : 0,
                    DiscountStartDate = model.IsDiscountActive ? model.DiscountStartDate : null,
                    DiscountEndDate = model.IsDiscountActive ? model.DiscountEndDate : null,
                    IsDiscountActive = model.IsDiscountActive,
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

                var responseMessage = model.IsDiscountActive ?
                    $"Menu item berhasil ditambahkan dengan diskon {model.DiscountPercentage}%" :
                    "Menu item berhasil ditambahkan";

                return Json(new
                {
                    success = true,
                    message = responseMessage,
                    hasDiscount = model.IsDiscountActive,
                    discountInfo = model.IsDiscountActive ? new
                    {
                        percentage = model.DiscountPercentage,
                        startDate = model.DiscountStartDate,
                        endDate = model.DiscountEndDate
                    } : null
                });
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

                // Enhanced response with complete discount info
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
                        isActive = menuItem.IsActive,
                        // Enhanced discount properties
                        discountPercentage = menuItem.DiscountPercentage ?? 0,
                        discountStartDate = menuItem.DiscountStartDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
                        discountEndDate = menuItem.DiscountEndDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
                        isDiscountActive = menuItem.IsDiscountActive,
                        // Additional discount info
                        hasActiveDiscount = menuItem.HasActiveDiscount,
                        finalPrice = menuItem.FinalPrice,
                        discountAmount = menuItem.DiscountAmount,
                        isDiscountValidNow = menuItem.IsDiscountValidForDate(DateTime.Now)
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

                // Enhanced discount validation
                if (model.IsDiscountActive)
                {
                    if (!model.DiscountPercentage.HasValue || model.DiscountPercentage <= 0)
                    {
                        return Json(new { success = false, message = "Persentase diskon harus lebih dari 0 jika diskon aktif" });
                    }

                    if (model.DiscountPercentage > 100)
                    {
                        return Json(new { success = false, message = "Persentase diskon tidak boleh lebih dari 100%" });
                    }

                    if (model.DiscountStartDate.HasValue && model.DiscountEndDate.HasValue)
                    {
                        if (model.DiscountEndDate < model.DiscountStartDate)
                        {
                            return Json(new { success = false, message = "Tanggal berakhir diskon harus setelah tanggal mulai" });
                        }
                    }
                }

                var menuItem = await _menuService.GetMenuItemByIdAsync(model.MenuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item tidak ditemukan" });
                }

                var oldStock = menuItem.Stock;
                var stockChanged = oldStock != model.Stock;
                var wasDiscountActive = menuItem.IsDiscountActive;
                var oldDiscountPercentage = menuItem.DiscountPercentage;

                // Update menu item with enhanced discount handling
                menuItem.CategoryId = model.CategoryId;
                menuItem.ItemName = model.ItemName;
                menuItem.Description = model.Description;
                menuItem.Price = model.Price;
                menuItem.Stock = model.Stock;
                menuItem.IsActive = model.IsActive;

                // Enhanced discount properties update
                menuItem.DiscountPercentage = model.IsDiscountActive ? (model.DiscountPercentage ?? 0) : 0;
                menuItem.DiscountStartDate = model.IsDiscountActive ? model.DiscountStartDate : null;
                menuItem.DiscountEndDate = model.IsDiscountActive ? model.DiscountEndDate : null;
                menuItem.IsDiscountActive = model.IsDiscountActive;
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

                // Create enhanced response message
                var responseMessage = "Menu item berhasil diperbarui";
                var discountStatusChanged = wasDiscountActive != model.IsDiscountActive;

                if (discountStatusChanged)
                {
                    if (model.IsDiscountActive)
                    {
                        responseMessage += $" dengan diskon {model.DiscountPercentage}%";
                    }
                    else
                    {
                        responseMessage += " (diskon dihapus)";
                    }
                }
                else if (model.IsDiscountActive && oldDiscountPercentage != model.DiscountPercentage)
                {
                    responseMessage += $" (diskon diperbarui ke {model.DiscountPercentage}%)";
                }

                return Json(new
                {
                    success = true,
                    message = responseMessage,
                    hasDiscount = model.IsDiscountActive,
                    discountChanged = discountStatusChanged,
                    discountInfo = model.IsDiscountActive ? new
                    {
                        percentage = model.DiscountPercentage,
                        startDate = model.DiscountStartDate,
                        endDate = model.DiscountEndDate,
                        finalPrice = menuItem.FinalPrice,
                        discountAmount = menuItem.DiscountAmount
                    } : null
                });
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