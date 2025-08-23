using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.HomeViewModels;
namespace POSRestoran01.Services.Interfaces
{
    public interface IOrderService
    {
        Task<string> GenerateOrderNumberAsync();
        Task<Order> CreateOrderAsync(PaymentViewModel paymentModel, int userId);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetOrdersByDateAsync(DateTime date);
        Task<Order> UpdateOrderStatusAsync(int orderId, string status);
        decimal CalculateSubtotal(List<OrderItemViewModel> items);
        decimal CalculatePPN(decimal subtotal, decimal discountAmount = 0);
        decimal CalculateTotal(decimal subtotal, decimal discount, decimal ppn);

        // Method untuk menghitung discount berdasarkan subtotal saja (tanpa percentage parameter)
        decimal CalculateDiscountAmount(decimal subtotal);

        // Method untuk mendapatkan discount percentage dari konfigurasi
        decimal GetCurrentDiscountPercentage();

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
    }
}