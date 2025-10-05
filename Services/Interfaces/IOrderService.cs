using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.HomeViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IOrderService
    {
        Task<string> GenerateOrderNumberAsync();
        Task<Order> CreateOrderAsync(PaymentViewModel paymentModel, int userId);
    
        Task<Order> CreateOrderWithMenuDiscountAsync(PaymentViewModel paymentModel, int userId, decimal menuDiscountTotal);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetOrdersByDateAsync(DateTime date);
        Task<Order> UpdateOrderStatusAsync(int orderId, string status);

       
        Task<List<Order>> GetOrdersByUserIdAsync(int userId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueByUserIdAsync(int userId, DateTime startDate, DateTime endDate);
        Task<int> GetTotalCustomersByUserIdAsync(int userId, DateTime startDate, DateTime endDate);
        Task<int> GetTotalMenusOrderedByUserIdAsync(int userId, DateTime startDate, DateTime endDate);

 
        decimal CalculateSubtotal(List<OrderItemViewModel> items);
        decimal CalculatePPN(decimal subtotal, decimal discountAmount = 0);
        decimal CalculateTotal(decimal subtotal, decimal discount, decimal ppn);
   
        decimal CalculateDiscountAmount(decimal subtotal);
        decimal GetCurrentDiscountPercentage();
   
        decimal CalculateSubtotalWithMenuDiscount(List<OrderItemViewModel> items);
        decimal CalculateOriginalSubtotal(List<OrderItemViewModel> items);
        decimal CalculateMenuDiscountTotal(List<OrderItemViewModel> items);
       
        (decimal subtotal, decimal discount, decimal menuDiscount, decimal ppn, decimal total) CalculateOrderTotalsWithMenuDiscount(
            List<OrderItemViewModel> items, bool applyOrderDiscount = false);

        Task<bool> ValidateOrderNumberAsync(string orderNumber);
        Task<List<Order>> GetRecentOrdersAsync(int count = 10);
        Task<decimal> GetTotalSalesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetTotalOrdersAsync(DateTime? date = null);
        Task<List<Order>> GetOrdersByStatusAsync(string status);
        Task<bool> CancelOrderAsync(int orderId, string reason = "");
        Task<OrderViewModel> ConvertToOrderViewModelAsync(Order order);
 
        Task<bool> IsOrderNumberExistsAsync(string orderNumber);
        Task<List<Order>> GetTodayOrdersAsync();
        Task<decimal> GetTodaySalesAsync();
        Task<Dictionary<string, object>> GetOrderStatisticsAsync(DateTime date);
        bool IsDiscountApplicable(decimal subtotal, decimal minimumAmount = 50000m);
     
        Task<Dictionary<string, object>> GetDiscountStatisticsAsync(DateTime startDate, DateTime endDate);
        bool ValidateDiscountEligibility(List<OrderItemViewModel> items, decimal minimumAmount = 50000m);
        List<(string name, decimal percentage, decimal minimumAmount)> GetAvailableDiscounts();
        decimal CalculateSavings(decimal subtotal);
        decimal CalculateTotalSavings(List<OrderItemViewModel> items, bool hasOrderDiscount = false);
    }
}