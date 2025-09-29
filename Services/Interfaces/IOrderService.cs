using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.HomeViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IOrderService
    {
        Task<string> GenerateOrderNumberAsync();
        Task<Order> CreateOrderAsync(PaymentViewModel paymentModel, int userId);
        // New method for creating order with menu discount support
        Task<Order> CreateOrderWithMenuDiscountAsync(PaymentViewModel paymentModel, int userId, decimal menuDiscountTotal);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetOrdersByDateAsync(DateTime date);
        Task<Order> UpdateOrderStatusAsync(int orderId, string status);

        // TAMBAH: Methods untuk Cashier Dashboard
        Task<List<Order>> GetOrdersByUserIdAsync(int userId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueByUserIdAsync(int userId, DateTime startDate, DateTime endDate);
        Task<int> GetTotalCustomersByUserIdAsync(int userId, DateTime startDate, DateTime endDate);
        Task<int> GetTotalMenusOrderedByUserIdAsync(int userId, DateTime startDate, DateTime endDate);

        // Existing calculation methods
        decimal CalculateSubtotal(List<OrderItemViewModel> items);
        decimal CalculatePPN(decimal subtotal, decimal discountAmount = 0);
        decimal CalculateTotal(decimal subtotal, decimal discount, decimal ppn);
        // Order discount methods
        decimal CalculateDiscountAmount(decimal subtotal);
        decimal GetCurrentDiscountPercentage();
        // New methods for menu discount support
        decimal CalculateSubtotalWithMenuDiscount(List<OrderItemViewModel> items);
        decimal CalculateOriginalSubtotal(List<OrderItemViewModel> items);
        decimal CalculateMenuDiscountTotal(List<OrderItemViewModel> items);
        // Enhanced calculation method with menu discount
        (decimal subtotal, decimal discount, decimal menuDiscount, decimal ppn, decimal total) CalculateOrderTotalsWithMenuDiscount(
            List<OrderItemViewModel> items, bool applyOrderDiscount = false);
        // Method tambahan
        Task<bool> ValidateOrderNumberAsync(string orderNumber);
        Task<List<Order>> GetRecentOrdersAsync(int count = 10);
        Task<decimal> GetTotalSalesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetTotalOrdersAsync(DateTime? date = null);
        Task<List<Order>> GetOrdersByStatusAsync(string status);
        Task<bool> CancelOrderAsync(int orderId, string reason = "");
        Task<OrderViewModel> ConvertToOrderViewModelAsync(Order order);
        // Helper methods
        Task<bool> IsOrderNumberExistsAsync(string orderNumber);
        Task<List<Order>> GetTodayOrdersAsync();
        Task<decimal> GetTodaySalesAsync();
        Task<Dictionary<string, object>> GetOrderStatisticsAsync(DateTime date);
        bool IsDiscountApplicable(decimal subtotal, decimal minimumAmount = 50000m);
        // Enhanced discount statistics with menu discount support
        Task<Dictionary<string, object>> GetDiscountStatisticsAsync(DateTime startDate, DateTime endDate);
        bool ValidateDiscountEligibility(List<OrderItemViewModel> items, decimal minimumAmount = 50000m);
        List<(string name, decimal percentage, decimal minimumAmount)> GetAvailableDiscounts();
        decimal CalculateSavings(decimal subtotal);
        decimal CalculateTotalSavings(List<OrderItemViewModel> items, bool hasOrderDiscount = false);
    }
}