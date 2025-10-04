using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models.ViewModels.DashboardViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(DateTime date)
        {
            var stats = await GetDashboardStatsAsync(date);
            var orderReports = await GetOrderReportsAsync(date, date, null, 1, 50);
            var popularMenus = await GetPopularMenusAsync(date, date, 10);
            var orderTypeStats = await GetOrderTypeStatsAsync(date, date);

            return new DashboardViewModel
            {
                SelectedDate = date,
                TotalRevenue = stats.TotalRevenue,
                TotalOrders = stats.TotalOrders,
                TotalCustomers = stats.TotalCustomers,
                TotalMenusOrdered = stats.TotalMenusOrdered,
                OrderReports = orderReports,
                PopularMenus = popularMenus,
                OrderTypeStats = orderTypeStats
            };
        }

        public async Task<DashboardViewModel> GetDashboardDataRangeAsync(DateTime startDate, DateTime endDate)
        {
            var stats = await GetDashboardStatsRangeAsync(startDate, endDate);
            var orderReports = await GetOrderReportsAsync(startDate, endDate, null, 1, 50);
            var popularMenus = await GetPopularMenusAsync(startDate, endDate, 10);
            var orderTypeStats = await GetOrderTypeStatsAsync(startDate, endDate);

            return new DashboardViewModel
            {
                SelectedDate = endDate,
                TotalRevenue = stats.TotalRevenue,
                TotalOrders = stats.TotalOrders,
                TotalCustomers = stats.TotalCustomers,
                TotalMenusOrdered = stats.TotalMenusOrdered,
                OrderReports = orderReports,
                PopularMenus = popularMenus,
                OrderTypeStats = orderTypeStats
            };
        }

        public async Task<List<OrderReportViewModel>> GetOrderReportsAsync(DateTime? startDate, DateTime? endDate, string? status, int page, int pageSize)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(o => o.OrderDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(o => o.OrderDate <= endDate.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return orders.Select(o => new OrderReportViewModel
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    MenuSummary = string.Join(", ", o.OrderDetails.Take(2).Select(od => $"{od.MenuItem?.ItemName} ({od.Quantity})")) +
                                  (o.OrderDetails.Count > 2 ? $" +{o.OrderDetails.Count - 2} lainnya" : ""),
                    Total = o.Total,
                    Status = o.Status,
                    OrderType = o.OrderType,
                    TableNo = o.TableNo,
                    OrderDate = o.OrderDate,
                    OrderTime = o.OrderTime,
                    UserName = o.User?.FullName ?? "Unknown"
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting order reports: {ex.Message}", ex);
            }
        }

        public async Task<List<PopularMenuViewModel>> GetPopularMenusAsync(DateTime? startDate, DateTime? endDate, int limit = 10)
        {
            try
            {
                var query = _context.OrderDetails
                    .Include(od => od.MenuItem)
                    .ThenInclude(m => m.Category)
                    .Include(od => od.Order)
                    .Where(od => od.Order.Status == "Completed")
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(od => od.Order.OrderDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(od => od.Order.OrderDate <= endDate.Value);

                var popularMenus = await query
                    .GroupBy(od => new {
                        od.MenuItemId,
                        od.MenuItem.ItemName,
                        CategoryName = od.MenuItem.Category != null ? od.MenuItem.Category.CategoryName : "No Category",
                        od.MenuItem.ImagePath
                    })
                    .Select(g => new PopularMenuViewModel
                    {
                        MenuItemId = g.Key.MenuItemId,
                        ItemName = g.Key.ItemName ?? "Unknown Item",
                        CategoryName = g.Key.CategoryName,
                        TotalOrdered = g.Sum(od => od.Quantity),
                        Revenue = g.Sum(od => od.Subtotal),
                        ImagePath = g.Key.ImagePath
                    })
                    .OrderByDescending(p => p.TotalOrdered)
                    .Take(limit)
                    .ToListAsync();

                return popularMenus;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting popular menus: {ex.Message}", ex);
            }
        }

        public async Task<List<OrderTypeStatsViewModel>> GetOrderTypeStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.Orders
                    .Where(o => o.Status == "Completed")
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(o => o.OrderDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(o => o.OrderDate <= endDate.Value);

                var totalOrders = await query.CountAsync();
                if (totalOrders == 0)
                    return new List<OrderTypeStatsViewModel>();

                var orderTypeStats = await query
                    .GroupBy(o => o.OrderType)
                    .Select(g => new
                    {
                        OrderType = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.Total)
                    })
                    .ToListAsync();

                return orderTypeStats.Select(stat => new OrderTypeStatsViewModel
                {
                    OrderType = stat.OrderType ?? "Unknown",
                    Count = stat.Count,
                    Percentage = Math.Round((decimal)stat.Count / totalOrders * 100, 1),
                    Revenue = stat.Revenue
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting order type stats: {ex.Message}", ex);
            }
        }

        public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(DateTime date)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.OrderDate == date)
                    .ToListAsync();

                return CalculateStats(orders);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting dashboard stats: {ex.Message}", ex);
            }
        }

        public async Task<DashboardStatsViewModel> GetDashboardStatsRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .ToListAsync();

                return CalculateStats(orders);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting dashboard stats range: {ex.Message}", ex);
            }
        }

        private DashboardStatsViewModel CalculateStats(List<POSRestoran01.Models.Order> orders)
        {
            var completedOrders = orders.Where(o => o.Status == "Completed").ToList();
            var totalRevenue = completedOrders.Sum(o => o.Total);
            var totalOrders = orders.Count;

           
            var totalMenusOrdered = completedOrders
                                    .SelectMany(o => o.OrderDetails)
                                    .Sum(od => od.Quantity);

            
            var totalCustomers = completedOrders.Count;

            return new DashboardStatsViewModel
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders.Count,
                CancelledOrders = orders.Count(o => o.Status == "Canceled"),
                PendingOrders = orders.Count(o => o.Status == "Pending" || o.Status == "Preparing"),
                AverageOrderValue = completedOrders.Any() ? completedOrders.Average(o => o.Total) : 0,
                TotalCustomers = totalCustomers,
                TotalMenusOrdered = totalMenusOrdered,
                DineInOrders = orders.Count(o => o.OrderType == "Dine In"),
                TakeAwayOrders = orders.Count(o => o.OrderType == "Take Away"),
                TotalDiscount = completedOrders.Sum(o => o.Discount)
            };
        }
    }
}