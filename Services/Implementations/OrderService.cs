using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.HomeViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly decimal _ppnRate = 0.11m; // 11%

        public OrderService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Property untuk mengambil discount percentage dari konfigurasi dengan default 5%
        private decimal DiscountPercentage =>
            _configuration.GetValue<decimal>("AppSettings:DiscountPercentage", 5m);

        public async Task<string> GenerateOrderNumberAsync()
        {
            var today = DateTime.Today;
            var orderCount = await _context.Orders
                .Where(o => o.OrderDate == today)
                .CountAsync();

            return $"ORD{today:yyyyMMdd}{(orderCount + 1):D3}";
        }

        public async Task<Order> CreateOrderAsync(PaymentViewModel paymentModel, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validasi stok sebelum membuat order
                foreach (var item in paymentModel.Items)
                {
                    var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                    if (menuItem == null)
                    {
                        throw new InvalidOperationException($"Menu item dengan ID {item.MenuItemId} tidak ditemukan");
                    }

                    if (!menuItem.IsActive)
                    {
                        throw new InvalidOperationException($"Menu item {menuItem.ItemName} tidak aktif");
                    }

                    if (menuItem.Stock < item.Quantity)
                    {
                        throw new InvalidOperationException($"Stok tidak mencukupi untuk {menuItem.ItemName}. Tersisa: {menuItem.Stock}");
                    }
                }

                var order = new Order
                {
                    OrderNumber = paymentModel.OrderNumber,
                    CustomerName = paymentModel.CustomerName,
                    OrderType = paymentModel.OrderType,
                    TableNo = paymentModel.TableNo,
                    OrderDate = DateTime.Today,
                    OrderTime = DateTime.Now.TimeOfDay,
                    Subtotal = paymentModel.Subtotal,
                    Discount = paymentModel.Discount,
                    PPN = paymentModel.PPN,
                    Total = paymentModel.Total,
                    Status = "Completed",
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order details dan update stock
                foreach (var item in paymentModel.Items)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        OrderNote = item.OrderNote,
                        Subtotal = item.Subtotal
                    };

                    _context.OrderDetails.Add(orderDetail);

                    // Update stock
                    var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                    if (menuItem != null)
                    {
                        menuItem.Stock -= item.Quantity;
                        menuItem.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetOrdersByDateAsync(DateTime date)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .Include(o => o.User)
                .Where(o => o.OrderDate == date)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return order!;
        }

        public decimal CalculateSubtotal(List<OrderItemViewModel> items)
        {
            return items.Sum(item => item.UnitPrice * item.Quantity);
        }

        public decimal CalculatePPN(decimal subtotal, decimal discountAmount = 0)
        {
            var taxableAmount = Math.Max(0, subtotal - discountAmount);
            return Math.Round(taxableAmount * _ppnRate, 0, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateTotal(decimal subtotal, decimal discount, decimal ppn)
        {
            return subtotal - discount + ppn;
        }

        // Method untuk menghitung discount berdasarkan subtotal dan discount yang diinginkan
        public decimal CalculateDiscountAmount(decimal subtotal)
        {
            if (subtotal <= 0)
                return 0;

            return Math.Round(subtotal * (DiscountPercentage / 100), 0, MidpointRounding.AwayFromZero);
        }

        // Method untuk mengecek apakah discount bisa diterapkan
        public bool IsDiscountApplicable(decimal subtotal, decimal minimumAmount = 50000m)
        {
            return subtotal >= minimumAmount;
        }

        // Method untuk mendapatkan discount percentage dari konfigurasi
        public decimal GetCurrentDiscountPercentage()
        {
            return DiscountPercentage;
        }

        // Method to calculate order totals with discount
        public (decimal subtotal, decimal discount, decimal ppn, decimal total) CalculateOrderTotals(
            List<OrderItemViewModel> items,
            bool applyDiscount = false)
        {
            var subtotal = CalculateSubtotal(items);
            var discount = applyDiscount ? CalculateDiscountAmount(subtotal) : 0;
            var ppn = CalculatePPN(subtotal, discount);
            var total = CalculateTotal(subtotal, discount, ppn);

            return (subtotal, discount, ppn, total);
        }

        // Method tambahan untuk mendukung HomeController
        public async Task<bool> ValidateOrderNumberAsync(string orderNumber)
        {
            return !await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetRecentOrdersAsync(int count = 10)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSalesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders.Where(o => o.Status == "Completed");

            if (startDate.HasValue)
                query = query.Where(o => o.OrderDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.OrderDate <= endDate.Value);

            return await query.SumAsync(o => o.Total);
        }

        public async Task<int> GetTotalOrdersAsync(DateTime? date = null)
        {
            if (date.HasValue)
            {
                return await _context.Orders
                    .CountAsync(o => o.OrderDate == date.Value);
            }

            return await _context.Orders.CountAsync();
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(string status)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.Status == "Canceled")
                    return false;

                // Restore stock
                foreach (var detail in order.OrderDetails)
                {
                    var menuItem = await _context.MenuItems.FindAsync(detail.MenuItemId);
                    if (menuItem != null)
                    {
                        menuItem.Stock += detail.Quantity;
                        menuItem.UpdatedAt = DateTime.Now;
                    }
                }

                order.Status = "Canceled";
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<OrderViewModel> ConvertToOrderViewModelAsync(Order order)
        {
            var orderViewModel = new OrderViewModel
            {
                OrderNumber = order.OrderNumber,
                Subtotal = order.Subtotal,
                Discount = order.Discount,
                PPN = order.PPN,
                Total = order.Total,
                Items = new List<OrderItemViewModel>()
            };

            foreach (var detail in order.OrderDetails)
            {
                orderViewModel.Items.Add(new OrderItemViewModel
                {
                    MenuItemId = detail.MenuItemId,
                    ItemName = detail.MenuItem?.ItemName ?? "",
                    ImagePath = detail.MenuItem?.ImagePath ?? "",
                    UnitPrice = detail.UnitPrice,
                    Quantity = detail.Quantity,
                    Subtotal = detail.Subtotal,
                    OrderNote = detail.OrderNote
                });
            }

            return orderViewModel;
        }

        // Helper methods tambahan
        public async Task<bool> IsOrderNumberExistsAsync(string orderNumber)
        {
            return await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetTodayOrdersAsync()
        {
            return await GetOrdersByDateAsync(DateTime.Today);
        }

        public async Task<decimal> GetTodaySalesAsync()
        {
            return await GetTotalSalesAsync(DateTime.Today, DateTime.Today);
        }

        public async Task<Dictionary<string, object>> GetOrderStatisticsAsync(DateTime date)
        {
            var orders = await GetOrdersByDateAsync(date);

            return new Dictionary<string, object>
            {
                { "TotalOrders", orders.Count },
                { "CompletedOrders", orders.Count(o => o.Status == "Completed") },
                { "CanceledOrders", orders.Count(o => o.Status == "Canceled") },
                { "TotalSales", orders.Where(o => o.Status == "Completed").Sum(o => o.Total) },
                { "AverageOrderValue", orders.Any() ? orders.Where(o => o.Status == "Completed").Average(o => o.Total) : 0 },
                { "DineInOrders", orders.Count(o => o.OrderType == "Dine In") },
                { "TakeAwayOrders", orders.Count(o => o.OrderType == "Take Away") },
                { "TotalDiscountGiven", orders.Where(o => o.Status == "Completed").Sum(o => o.Discount) },
                { "OrdersWithDiscount", orders.Count(o => o.Status == "Completed" && o.Discount > 0) },
                { "CurrentDiscountPercentage", DiscountPercentage }
            };
        }

        // Method to get discount statistics
        public async Task<Dictionary<string, object>> GetDiscountStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                .ToListAsync();

            var totalOrders = orders.Count;
            var ordersWithDiscount = orders.Count(o => o.Discount > 0);
            var totalDiscountGiven = orders.Sum(o => o.Discount);
            var averageDiscountPerOrder = ordersWithDiscount > 0 ? totalDiscountGiven / ordersWithDiscount : 0;

            return new Dictionary<string, object>
            {
                { "TotalOrders", totalOrders },
                { "OrdersWithDiscount", ordersWithDiscount },
                { "DiscountPercentage", totalOrders > 0 ? (decimal)ordersWithDiscount / totalOrders * 100 : 0 },
                { "TotalDiscountAmount", totalDiscountGiven },
                { "AverageDiscountPerOrder", averageDiscountPerOrder },
                { "TotalSavings", totalDiscountGiven },
                { "CurrentDiscountPercentage", DiscountPercentage }
            };
        }

        // Method to validate discount eligibility
        public bool ValidateDiscountEligibility(List<OrderItemViewModel> items, decimal minimumAmount = 50000m)
        {
            var subtotal = CalculateSubtotal(items);
            return IsDiscountApplicable(subtotal, minimumAmount);
        }

        // Method to get available discounts
        public List<(string name, decimal percentage, decimal minimumAmount)> GetAvailableDiscounts()
        {
            return new List<(string, decimal, decimal)>
            {
                ($"Diskon {DiscountPercentage}%", DiscountPercentage, 50000m)
            };
        }

        // Method to calculate savings from discount
        public decimal CalculateSavings(decimal subtotal)
        {
            return CalculateDiscountAmount(subtotal);
        }
    }
}