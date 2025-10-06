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
        private readonly IStockHistoryService _stockHistoryService;
        private readonly decimal _ppnRate = 0.11m; 


        public OrderService(ApplicationDbContext context, IConfiguration configuration, IStockHistoryService stockHistoryService) 
        {
            _context = context;
            _configuration = configuration;
            _stockHistoryService = stockHistoryService; 
        }

        
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
            return await CreateOrderWithMenuDiscountAsync(paymentModel, userId, 0);
        }

        public async Task<Order> CreateOrderWithMenuDiscountAsync(PaymentViewModel paymentModel, int userId, decimal menuDiscountTotal)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
              
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
                    MenuDiscountTotal = menuDiscountTotal, 
                    PPN = paymentModel.PPN,
                    Total = paymentModel.Total,
                    Status = "Completed",
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                
                foreach (var item in paymentModel.Items)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        OriginalPrice = item.OriginalPrice > 0 ? item.OriginalPrice : item.UnitPrice, 
                        DiscountPercentage = item.DiscountPercentage,
                        DiscountAmount = item.DiscountAmount,
                        OrderNote = item.OrderNote,
                        Subtotal = item.Subtotal
                    };

                    _context.OrderDetails.Add(orderDetail);

                    
                    var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                    if (menuItem != null)
                    {
                        var previousStock = menuItem.Stock;
                        var newStock = previousStock - item.Quantity;

                       
                        await _stockHistoryService.RecordStockChangeAsync(
                            item.MenuItemId,
                            userId,
                            previousStock,
                            newStock,
                            "Order Reduction",
                            $"Order: {paymentModel.OrderNumber} - {item.ItemName} x{item.Quantity}"
                        );

                        
                        menuItem.Stock = newStock;
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

       
        public decimal CalculateSubtotalWithMenuDiscount(List<OrderItemViewModel> items)
        {
            return items.Sum(item => item.UnitPrice * item.Quantity); 
        }

        
        public decimal CalculateOriginalSubtotal(List<OrderItemViewModel> items)
        {
            return items.Sum(item => (item.OriginalPrice > 0 ? item.OriginalPrice : item.UnitPrice) * item.Quantity);
        }

        
        public decimal CalculateMenuDiscountTotal(List<OrderItemViewModel> items)
        {
            return items.Where(item => item.HasDiscount)
                       .Sum(item => item.DiscountAmount * item.Quantity);
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

        
        public decimal CalculateDiscountAmount(decimal subtotal)
        {
            if (subtotal <= 0)
                return 0;

            return Math.Round(subtotal * (DiscountPercentage / 100), 0, MidpointRounding.AwayFromZero);
        }

        
        public bool IsDiscountApplicable(decimal subtotal, decimal minimumAmount = 50000m)
        {
            return subtotal >= minimumAmount;
        }


        public decimal GetCurrentDiscountPercentage()
        {
            return DiscountPercentage;
        }

    
        public (decimal subtotal, decimal discount, decimal menuDiscount, decimal ppn, decimal total) CalculateOrderTotalsWithMenuDiscount(
            List<OrderItemViewModel> items,
            bool applyOrderDiscount = false)
        {
            var subtotal = CalculateSubtotalWithMenuDiscount(items);
            var menuDiscountTotal = CalculateMenuDiscountTotal(items);
            var orderDiscount = applyOrderDiscount ? CalculateDiscountAmount(subtotal) : 0;
            var ppn = CalculatePPN(subtotal, orderDiscount);
            var total = CalculateTotal(subtotal, orderDiscount, ppn);

            return (subtotal, orderDiscount, menuDiscountTotal, ppn, total);
        }

    
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

               
                foreach (var detail in order.OrderDetails)
                {
                    var menuItem = await _context.MenuItems.FindAsync(detail.MenuItemId);
                    if (menuItem != null)
                    {
                        var previousStock = menuItem.Stock;
                        var newStock = previousStock + detail.Quantity;

                     
                        await _stockHistoryService.RecordStockChangeAsync(
                            detail.MenuItemId,
                            order.UserId,
                            previousStock,
                            newStock,
                            "Order Cancellation",
                            $"Order Canceled: {order.OrderNumber} - {menuItem.ItemName} x{detail.Quantity}"
                        );

                        menuItem.Stock = newStock;
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
                MenuDiscountTotal = order.MenuDiscountTotal,
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
                    OriginalPrice = detail.OriginalPrice,
                    DiscountPercentage = detail.DiscountPercentage,
                    DiscountAmount = detail.DiscountAmount,
                    HasDiscount = detail.HasDiscount,
                    Quantity = detail.Quantity,
                    Subtotal = detail.Subtotal,
                    OrderNote = detail.OrderNote
                });
            }

            return orderViewModel;
        }


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
                { "TotalOrderDiscount", orders.Where(o => o.Status == "Completed").Sum(o => o.Discount) },
                { "TotalMenuDiscount", orders.Where(o => o.Status == "Completed").Sum(o => o.MenuDiscountTotal) },
                { "TotalDiscountGiven", orders.Where(o => o.Status == "Completed").Sum(o => o.Discount + o.MenuDiscountTotal) },
                { "OrdersWithDiscount", orders.Count(o => o.Status == "Completed" && (o.Discount > 0 || o.MenuDiscountTotal > 0)) },
                { "CurrentDiscountPercentage", DiscountPercentage }
            };
        }

        
        public async Task<Dictionary<string, object>> GetDiscountStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                .ToListAsync();

            var totalOrders = orders.Count;
            var ordersWithOrderDiscount = orders.Count(o => o.Discount > 0);
            var ordersWithMenuDiscount = orders.Count(o => o.MenuDiscountTotal > 0);
            var ordersWithAnyDiscount = orders.Count(o => o.Discount > 0 || o.MenuDiscountTotal > 0);
            var totalOrderDiscountGiven = orders.Sum(o => o.Discount);
            var totalMenuDiscountGiven = orders.Sum(o => o.MenuDiscountTotal);
            var totalDiscountGiven = totalOrderDiscountGiven + totalMenuDiscountGiven;

            return new Dictionary<string, object>
            {
                { "TotalOrders", totalOrders },
                { "OrdersWithOrderDiscount", ordersWithOrderDiscount },
                { "OrdersWithMenuDiscount", ordersWithMenuDiscount },
                { "OrdersWithAnyDiscount", ordersWithAnyDiscount },
                { "DiscountPercentage", totalOrders > 0 ? (decimal)ordersWithAnyDiscount / totalOrders * 100 : 0 },
                { "TotalOrderDiscountAmount", totalOrderDiscountGiven },
                { "TotalMenuDiscountAmount", totalMenuDiscountGiven },
                { "TotalDiscountAmount", totalDiscountGiven },
                { "AverageDiscountPerOrder", ordersWithAnyDiscount > 0 ? totalDiscountGiven / ordersWithAnyDiscount : 0 },
                { "TotalSavings", totalDiscountGiven },
                { "CurrentDiscountPercentage", DiscountPercentage }
            };
        }

        
        public bool ValidateDiscountEligibility(List<OrderItemViewModel> items, decimal minimumAmount = 50000m)
        {
            var subtotal = CalculateSubtotal(items);
            return IsDiscountApplicable(subtotal, minimumAmount);
        }


        public List<(string name, decimal percentage, decimal minimumAmount)> GetAvailableDiscounts()
        {
            return new List<(string, decimal, decimal)>
            {
                ($"Diskon Order {DiscountPercentage}%", DiscountPercentage, 50000m)
            };
        }

        public decimal CalculateSavings(decimal subtotal)
        {
            return CalculateDiscountAmount(subtotal);
        }

      
        public decimal CalculateTotalSavings(List<OrderItemViewModel> items, bool hasOrderDiscount = false)
        {
            var menuDiscountTotal = CalculateMenuDiscountTotal(items);
            var orderDiscountTotal = hasOrderDiscount ? CalculateDiscountAmount(CalculateSubtotal(items)) : 0;
            return menuDiscountTotal + orderDiscountTotal;
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .Where(o => o.UserId == userId &&
                            o.OrderDate >= startDate.Date &&
                            o.OrderDate <= endDate.Date)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }


        public async Task<decimal> GetTotalRevenueByUserIdAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId &&
                            o.OrderDate >= startDate.Date &&
                            o.OrderDate <= endDate.Date &&
                            o.Status == "Completed")
                .SumAsync(o => o.Total);
        }

 
        public async Task<int> GetTotalCustomersByUserIdAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId &&
                            o.OrderDate >= startDate.Date &&
                            o.OrderDate <= endDate.Date &&
                            o.Status == "Completed")
                .CountAsync();
        }

        
        public async Task<int> GetTotalMenusOrderedByUserIdAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId &&
                            o.OrderDate >= startDate.Date &&
                            o.OrderDate <= endDate.Date &&
                            o.Status == "Completed")
                .SelectMany(o => o.OrderDetails)
                .SumAsync(od => od.Quantity);
        }
    }
}