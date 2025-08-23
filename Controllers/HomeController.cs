using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Models.ViewModels.HomeViewModels;
using POSRestoran01.Services.Interfaces;
using System.Text.Json;

namespace POSRestoran01.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ICategoryService _categoryService;
        private readonly IMenuService _menuService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public HomeController(
            ICategoryService categoryService,
            IMenuService menuService,
            IOrderService orderService,
            IPaymentService paymentService,
            IConfiguration configuration)
        {
            _categoryService = categoryService;
            _menuService = menuService;
            _orderService = orderService;
            _paymentService = paymentService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(int? categoryId, string? searchTerm)
        {
            try
            {
                var model = new POSViewModel
                {
                    RestaurantName = _configuration["AppSettings:RestaurantName"] ?? "POS Restoran",
                    CurrentDate = DateTime.Now,
                    Categories = await _categoryService.GetActiveCategoriesAsync(),
                    SelectedCategoryId = categoryId ?? 0,
                    SearchTerm = searchTerm ?? string.Empty
                };

                // Load menu items based on search or category
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    model.MenuItems = await _menuService.SearchMenuItemsAsync(searchTerm);
                }
                else if (categoryId.HasValue && categoryId.Value > 0)
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
                    model.MenuItems = new List<POSRestoran01.Models.MenuItem>();
                }

                // Get or initialize current order from session
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (!string.IsNullOrEmpty(orderJson))
                {
                    try
                    {
                        model.CurrentOrder = JsonSerializer.Deserialize<OrderViewModel>(orderJson) ?? new OrderViewModel();
                    }
                    catch (JsonException)
                    {
                        // If deserialize fails, create new order
                        model.CurrentOrder = new OrderViewModel();
                        HttpContext.Session.Remove("CurrentOrder");
                    }
                }
                else
                {
                    model.CurrentOrder = new OrderViewModel();
                }

                // Generate order number if not exists
                if (string.IsNullOrEmpty(model.CurrentOrder.OrderNumber))
                {
                    model.CurrentOrder.OrderNumber = await _orderService.GenerateOrderNumberAsync();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // Log error (implement your logging here)
                TempData["Error"] = "Terjadi kesalahan saat memuat halaman.";
                return View(new POSViewModel());
            }
        }

        // Add new action for getting menu by category via AJAX
        [HttpGet]
        public async Task<IActionResult> GetMenuByCategory(int categoryId)
        {
            try
            {
                var menuItems = await _menuService.GetMenuItemsByCategoryAsync(categoryId);
                return PartialView("_MenuItemsPartial", menuItems);
            }
            catch (Exception)
            {
                return BadRequest("Terjadi kesalahan saat memuat menu");
            }
        }

        // Add new action for searching menu items via AJAX
        [HttpGet]
        public async Task<IActionResult> SearchMenuItems(string searchTerm)
        {
            try
            {
                var menuItems = !string.IsNullOrEmpty(searchTerm)
                    ? await _menuService.SearchMenuItemsAsync(searchTerm)
                    : new List<POSRestoran01.Models.MenuItem>();

                return PartialView("_MenuItemsPartial", menuItems);
            }
            catch (Exception)
            {
                return BadRequest("Terjadi kesalahan saat mencari menu");
            }
        }

        // Add new action for getting current order via AJAX
        [HttpGet]
        public IActionResult GetCurrentOrder()
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                OrderViewModel order;

                if (!string.IsNullOrEmpty(orderJson))
                {
                    try
                    {
                        order = JsonSerializer.Deserialize<OrderViewModel>(orderJson) ?? new OrderViewModel();
                    }
                    catch (JsonException)
                    {
                        order = new OrderViewModel();
                        HttpContext.Session.Remove("CurrentOrder");
                    }
                }
                else
                {
                    order = new OrderViewModel();
                }

                return PartialView("_OrderSummaryPartial", order);
            }
            catch (Exception)
            {
                return BadRequest("Terjadi kesalahan saat memuat order");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToOrder(int menuItemId, int quantity = 1, string? note = null)
        {
            try
            {
                // Validate input
                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity harus lebih dari 0" });
                }

                var menuItem = await _menuService.GetMenuItemByIdAsync(menuItemId);
                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Menu tidak ditemukan" });
                }

                if (!menuItem.IsActive)
                {
                    return Json(new { success = false, message = "Menu tidak tersedia" });
                }

                if (menuItem.Stock < quantity)
                {
                    return Json(new { success = false, message = $"Stok tidak mencukupi. Tersisa {menuItem.Stock}" });
                }

                // Get current order from session
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                OrderViewModel order;

                if (string.IsNullOrEmpty(orderJson))
                {
                    order = new OrderViewModel
                    {
                        OrderNumber = await _orderService.GenerateOrderNumberAsync()
                    };
                }
                else
                {
                    try
                    {
                        order = JsonSerializer.Deserialize<OrderViewModel>(orderJson) ?? new OrderViewModel();
                    }
                    catch (JsonException)
                    {
                        order = new OrderViewModel
                        {
                            OrderNumber = await _orderService.GenerateOrderNumberAsync()
                        };
                    }
                }

                // Check if item already exists in order
                var existingItem = order.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (existingItem != null)
                {
                    // Check total quantity doesn't exceed stock
                    var newQuantity = existingItem.Quantity + quantity;
                    if (newQuantity > menuItem.Stock)
                    {
                        return Json(new { success = false, message = $"Total quantity akan melebihi stok. Maksimal {menuItem.Stock}" });
                    }

                    existingItem.Quantity = newQuantity;
                    existingItem.Subtotal = existingItem.Quantity * existingItem.UnitPrice;

                    // Update note if provided
                    if (!string.IsNullOrEmpty(note))
                    {
                        existingItem.OrderNote = note;
                    }
                }
                else
                {
                    order.Items.Add(new OrderItemViewModel
                    {
                        MenuItemId = menuItemId,
                        ItemName = menuItem.ItemName,
                        ImagePath = menuItem.ImagePath,
                        UnitPrice = menuItem.Price,
                        Quantity = quantity,
                        Subtotal = menuItem.Price * quantity,
                        OrderNote = note
                    });
                }

                // Recalculate totals
                RecalculateOrderTotals(order);

                // Save to session
                var updatedOrderJson = JsonSerializer.Serialize(order);
                HttpContext.Session.SetString("CurrentOrder", updatedOrderJson);

                return Json(new
                {
                    success = true,
                    message = $"{menuItem.ItemName} berhasil ditambahkan",
                    totalItems = order.Items.Sum(i => i.Quantity)
                });
            }
            catch (Exception ex)
            {
                // Log error
                return Json(new { success = false, message = "Terjadi kesalahan saat menambahkan item" });
            }
        }

        // Add new action for removing from order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromOrder(int menuItemId)
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (string.IsNullOrEmpty(orderJson))
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                var order = JsonSerializer.Deserialize<OrderViewModel>(orderJson);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order tidak valid" });
                }

                var item = order.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item tidak ditemukan dalam order" });
                }

                order.Items.Remove(item);

                // Recalculate totals
                RecalculateOrderTotals(order);

                // Save to session
                HttpContext.Session.SetString("CurrentOrder", JsonSerializer.Serialize(order));

                return Json(new { success = true, message = "Item berhasil dihapus", totalItems = order.Items.Sum(i => i.Quantity) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus item" });
            }
        }

        // Add new action for clearing order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearOrder()
        {
            try
            {
                HttpContext.Session.Remove("CurrentOrder");
                return Json(new { success = true, message = "Order berhasil dihapus" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus order" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderItemQuantity(int menuItemId, int quantity)
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (string.IsNullOrEmpty(orderJson))
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                var order = JsonSerializer.Deserialize<OrderViewModel>(orderJson);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order tidak valid" });
                }

                var item = order.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item tidak ditemukan dalam order" });
                }

                if (quantity <= 0)
                {
                    // Remove item if quantity is 0 or less
                    order.Items.Remove(item);
                }
                else
                {
                    // Update quantity
                    item.Quantity = quantity;
                    item.Subtotal = item.Quantity * item.UnitPrice;
                }

                // Recalculate totals
                RecalculateOrderTotals(order);

                // Save to session
                HttpContext.Session.SetString("CurrentOrder", JsonSerializer.Serialize(order));

                return Json(new { success = true, totalItems = order.Items.Sum(i => i.Quantity) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat mengupdate quantity" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateItemNote(int menuItemId, string? note)
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (string.IsNullOrEmpty(orderJson))
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                var order = JsonSerializer.Deserialize<OrderViewModel>(orderJson);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order tidak valid" });
                }

                var item = order.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item tidak ditemukan dalam order" });
                }

                item.OrderNote = note;

                // Save to session
                HttpContext.Session.SetString("CurrentOrder", JsonSerializer.Serialize(order));

                return Json(new { success = true, message = "Catatan item berhasil diperbarui" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat mengupdate catatan" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyDiscount(bool applyDiscount = false)
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (string.IsNullOrEmpty(orderJson))
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                var order = JsonSerializer.Deserialize<OrderViewModel>(orderJson);
                if (order == null || !order.Items.Any())
                {
                    return Json(new { success = false, message = "Order kosong" });
                }

                // Apply atau remove discount
                if (applyDiscount)
                {
                    order.Discount = _orderService.CalculateDiscountAmount(order.Subtotal);
                }
                else
                {
                    order.Discount = 0m;
                }

                // Recalculate totals
                RecalculateOrderTotals(order);

                // Save to session
                HttpContext.Session.SetString("CurrentOrder", JsonSerializer.Serialize(order));

                var discountPercentage = _orderService.GetCurrentDiscountPercentage();
                return Json(new
                {
                    success = true,
                    message = applyDiscount ? $"Diskon {discountPercentage}% berhasil diterapkan" : "Diskon berhasil dihapus",
                    discount = order.Discount,
                    discountPercentage = discountPercentage,
                    total = order.Total
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat menerapkan diskon" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
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

                // Get current order from session
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (string.IsNullOrEmpty(orderJson))
                {
                    return Json(new { success = false, message = "Tidak ada order yang aktif" });
                }

                var order = JsonSerializer.Deserialize<OrderViewModel>(orderJson);
                if (order == null || !order.Items.Any())
                {
                    return Json(new { success = false, message = "Order kosong" });
                }

                // Set payment data from current order with proper calculations
                model.Items = order.Items;
                model.OrderNumber = order.OrderNumber;
                model.Subtotal = _orderService.CalculateSubtotal(order.Items);
                model.Discount = order.Discount;
                model.PPN = _orderService.CalculatePPN(model.Subtotal, model.Discount);
                model.Total = _orderService.CalculateTotal(model.Subtotal, model.Discount, model.PPN);
                model.Change = _paymentService.CalculateChange(model.Cash, model.Total);

                // Validate cash amount
                if (model.Cash < model.Total)
                {
                    return Json(new { success = false, message = "Jumlah cash tidak mencukupi" });
                }

                // Validate table number for dine in
                if (model.OrderType == "Dine In" && (!model.TableNo.HasValue || model.TableNo <= 0))
                {
                    return Json(new { success = false, message = "Nomor meja harus diisi untuk Dine In" });
                }

                // Get user ID from session
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "Session tidak valid. Silakan login ulang." });
                }

                // Create order
                var createdOrder = await _orderService.CreateOrderAsync(model, userId);

                // Process payment
                await _paymentService.ProcessPaymentAsync(model, createdOrder.OrderId);

                // Clear session
                HttpContext.Session.Remove("CurrentOrder");

                return Json(new
                {
                    success = true,
                    orderId = createdOrder.OrderId,
                    orderNumber = createdOrder.OrderNumber,
                    message = "Pembayaran berhasil diproses"
                });
            }
            catch (Exception ex)
            {
                // Log error
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        // Helper method to recalculate order totals
        private void RecalculateOrderTotals(OrderViewModel order)
        {
            order.Subtotal = _orderService.CalculateSubtotal(order.Items);
            // Keep existing discount if it was applied (hanya gunakan nilai Discount yang ada)
            // Tidak menggunakan DiscountPercentage lagi
            order.PPN = _orderService.CalculatePPN(order.Subtotal, order.Discount);
            order.Total = _orderService.CalculateTotal(order.Subtotal, order.Discount, order.PPN);
        }

        // API endpoint for getting order summary data (for AJAX calls)
        [HttpGet]
        public IActionResult GetOrderSummaryData()
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("CurrentOrder");
                if (string.IsNullOrEmpty(orderJson))
                {
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            totalItems = 0,
                            total = 0,
                            isEmpty = true
                        }
                    });
                }

                var order = JsonSerializer.Deserialize<OrderViewModel>(orderJson);
                if (order == null)
                {
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            totalItems = 0,
                            total = 0,
                            isEmpty = true
                        }
                    });
                }

                var discountPercentage = _orderService.GetCurrentDiscountPercentage();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalItems = order.Items.Sum(i => i.Quantity),
                        total = order.Total,
                        subtotal = order.Subtotal,
                        ppn = order.PPN,
                        discount = order.Discount,
                        discountPercentage = discountPercentage,
                        hasDiscount = order.Discount > 0,
                        isEmpty = !order.Items.Any(),
                        orderNumber = order.OrderNumber
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan" });
            }
        }
    }
}