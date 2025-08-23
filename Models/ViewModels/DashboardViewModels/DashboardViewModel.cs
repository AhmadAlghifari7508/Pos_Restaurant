using POSRestoran01.Models;

namespace POSRestoran01.Models.ViewModels.DashboardViewModels
{
    public class DashboardViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalMenusOrdered { get; set; }
        public List<OrderReportViewModel> OrderReports { get; set; } = new List<OrderReportViewModel>();
        public List<PopularMenuViewModel> PopularMenus { get; set; } = new List<PopularMenuViewModel>();
        public List<OrderTypeStatsViewModel> OrderTypeStats { get; set; } = new List<OrderTypeStatsViewModel>();
    }

    public class OrderReportViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string MenuSummary { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public int? TableNo { get; set; }
        public DateTime OrderDate { get; set; }
        public TimeSpan OrderTime { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class OrderDetailViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string OrderNote { get; set; } = string.Empty;
    }


    public class PopularMenuViewModel
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalOrdered { get; set; }
        public decimal Revenue { get; set; }
        public string? ImagePath { get; set; }
    }

    public class OrderTypeStatsViewModel
    {
        public string OrderType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardStatsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalMenusOrdered { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int DineInOrders { get; set; }
        public int TakeAwayOrders { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}