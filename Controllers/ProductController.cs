using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.ProductViewModels;
using POSRestoran01.Services.Interfaces;

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

        // GET: Product/Index
        public async Task<IActionResult> Index(int? categoryId)
        {
            try
            {
                var model = new ProductManagementViewModel
                {
                    Categories = await _categoryService.GetActiveCategoriesAsync(),
                    SelectedCategoryId = categoryId ?? 0
                };

                // Show ALL menus (Active and Inactive) in Product Management
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    model.MenuItems = await _menuService.GetMenuItemsByCategoryAsync(categoryId.Value);
                }
                else
                {
                    model.MenuItems = await _menuService.GetAllMenuItemsAsync();
                    model.SelectedCategoryId = 0;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Product/Index: {ex.Message}");
                TempData["Error"] = "Terjadi kesalahan saat memuat halaman Product Management.";
                return View(new ProductManagementViewModel());
            }
        }

        // GET: Product/GetMenuByCategory
        [HttpGet]
        public async Task<IActionResult> GetMenuByCategory(int categoryId)
        {
            try
            {
                // Show ALL menus (Active and Inactive)
                var menuItems = categoryId == 0
                    ? await _menuService.GetAllMenuItemsAsync()
                    : await _menuService.GetMenuItemsByCategoryAsync(categoryId);

                return PartialView("_ProductMenuItemsPartial", menuItems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMenuByCategory: {ex.Message}");
                return BadRequest("Terjadi kesalahan saat memuat menu");
            }
        }

        // GET: Product/GetCategories
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

        // POST: Product/CreateCategory
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

        // POST: Product/UpdateCategory
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

        // POST: Product/DeleteCategory
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

        // POST: Product/CreateMenuItem
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

                // Validate discount settings
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

                // Handle image upload
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
                    DiscountPercentage = model.IsDiscountActive ? (model.DiscountPercentage ?? 0) : 0,
                    DiscountStartDate = model.IsDiscountActive ? model.DiscountStartDate : null,
                    DiscountEndDate = model.IsDiscountActive ? model.DiscountEndDate : null,
                    IsDiscountActive = model.IsDiscountActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _menuService.CreateMenuItemAsync(menuItem);

                // Record initial stock
                await _stockHistoryService.RecordStockChangeAsync(
                    menuItem.MenuItemId,
                    GetCurrentUserId(),
                    0,
                    menuItem.Stock,
                    "Initial Stock",
                    $"Initial stock for new menu item: {menuItem.ItemName}"
                );

                var responseMessage = model.IsDiscountActive
                    ? $"Menu '{menuItem.ItemName}' berhasil ditambahkan dengan diskon {model.DiscountPercentage}%"
                    : $"Menu '{menuItem.ItemName}' berhasil ditambahkan";

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

        // GET: Product/GetMenuItem
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
                        discountPercentage = menuItem.DiscountPercentage ?? 0,
                        discountStartDate = menuItem.DiscountStartDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
                        discountEndDate = menuItem.DiscountEndDate?.ToString("yyyy-MM-ddTHH:mm") ?? "",
                        isDiscountActive = menuItem.IsDiscountActive,
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

        // POST: Product/UpdateMenuItem
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

                // Validate discount settings
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

                // Update menu item properties
                menuItem.CategoryId = model.CategoryId;
                menuItem.ItemName = model.ItemName;
                menuItem.Description = model.Description;
                menuItem.Price = model.Price;
                menuItem.Stock = model.Stock;
                menuItem.IsActive = model.IsActive;

                menuItem.DiscountPercentage = model.IsDiscountActive ? (model.DiscountPercentage ?? 0) : 0;
                menuItem.DiscountStartDate = model.IsDiscountActive ? model.DiscountStartDate : null;
                menuItem.DiscountEndDate = model.IsDiscountActive ? model.DiscountEndDate : null;
                menuItem.IsDiscountActive = model.IsDiscountActive;
                menuItem.UpdatedAt = DateTime.Now;

                // Handle image update
                if (model.ImageFile != null)
                {
                    if (!string.IsNullOrEmpty(menuItem.ImagePath))
                    {
                        DeleteImage(menuItem.ImagePath);
                    }
                    menuItem.ImagePath = await SaveImageAsync(model.ImageFile);
                }

                await _menuService.UpdateMenuItemAsync(menuItem);

                // Record stock changes
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

                // Build response message
                var responseMessage = $"Menu '{menuItem.ItemName}' berhasil diperbarui";
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

                if (!model.IsActive && wasDiscountActive != model.IsDiscountActive)
                {
                    responseMessage += " - Menu dinonaktifkan";
                }

                return Json(new
                {
                    success = true,
                    message = responseMessage,
                    hasDiscount = model.IsDiscountActive,
                    discountChanged = discountStatusChanged,
                    isActive = model.IsActive,
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

        // POST: Product/DeleteMenuItem - HARD DELETE (Permanent)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItem(int menuItemId)
        {
            try
            {
                var menuItem = await _menuService.GetMenuItemByIdAsync(menuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu item tidak ditemukan" });
                }

                var menuName = menuItem.ItemName;

                // Delete image file if exists
                if (!string.IsNullOrEmpty(menuItem.ImagePath))
                {
                    DeleteImage(menuItem.ImagePath);
                }

                // HARD DELETE - permanently remove from database
                var success = await _menuService.DeleteMenuItemAsync(menuItemId);

                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Menu '{menuName}' berhasil dihapus PERMANENT dari database"
                    });
                }

                return Json(new { success = false, message = "Gagal menghapus menu" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteMenuItem: {ex.Message}");
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        // Private Helper Methods
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "menu");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
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

            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
            }
        }
    }
}