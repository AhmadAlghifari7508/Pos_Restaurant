using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Models.ViewModels.DashboardViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly IOrderService _orderService;

        public DashboardController(
            IDashboardService dashboardService,
            IOrderService orderService)
        {
            _dashboardService = dashboardService;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            try
            {
                var selectedDate = date ?? DateTime.Today;
                var model = await _dashboardService.GetDashboardDataAsync(selectedDate);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Terjadi kesalahan saat memuat dashboard.";
                return View(new DashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderReports(DateTime? startDate, DateTime? endDate, string? status, int page = 1, int pageSize = 10)
        {
            try
            {
                // Limit to show only 3 most recent orders for initial view
                var reports = await _dashboardService.GetOrderReportsAsync(startDate, endDate, status, page, Math.Min(pageSize, 10));
                return Json(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat laporan order: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var validStatuses = new[] { "Pending", "Preparing", "Completed", "Canceled" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Status tidak valid" });
                }

                var order = await _orderService.UpdateOrderStatusAsync(orderId, status);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                return Json(new { success = true, message = $"Status order berhasil diubah menjadi {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat mengubah status order: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order tidak ditemukan" });
                }

                var orderDetails = new
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    CustomerName = order.CustomerName,
                    OrderType = order.OrderType,
                    TableNo = order.TableNo,
                    OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                    OrderTime = order.OrderTime.ToString(@"hh\:mm"),
                    Status = order.Status,
                    Cashier = order.User?.FullName ?? "Unknown",
                    Subtotal = order.Subtotal,
                    Discount = order.Discount,
                    PPN = order.PPN,
                    Total = order.Total,
                    Items = order.OrderDetails?
                    .Select(od => new OrderDetailViewModel
                    {
                        ItemName = od.MenuItem?.ItemName ?? "Item tidak diketahui",
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        Subtotal = od.Subtotal,
                        OrderNote = od.OrderNote ?? string.Empty
                    }).ToList() ?? new List<OrderDetailViewModel>()
                };

                return Json(new { success = true, data = orderDetails });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat detail order: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPopularMenus(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Limit to top 10 items
                var popularMenus = await _dashboardService.GetPopularMenusAsync(startDate, endDate, 10);
                return Json(new { success = true, data = popularMenus });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat menu populer: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderTypeStats(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var orderTypeStats = await _dashboardService.GetOrderTypeStatsAsync(startDate, endDate);
                return Json(new { success = true, data = orderTypeStats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat statistik tipe order: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // If no date range provided, use today
                var start = startDate ?? DateTime.Today;
                var end = endDate ?? DateTime.Today;

                var stats = await _dashboardService.GetDashboardStatsRangeAsync(start, end);
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat data dashboard: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrderReport(DateTime? startDate, DateTime? endDate, string? status)
        {
            try
            {
                // This would typically generate and return a file (CSV/Excel)
                // For now, return success message
                return Json(new { success = true, message = "Export berhasil (fitur akan diimplementasi)" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat export laporan: " + ex.Message });
            }
        }
    }
}