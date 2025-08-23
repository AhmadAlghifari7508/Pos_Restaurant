using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.HomeViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(PaymentViewModel paymentModel, int orderId);
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
        decimal CalculateChange(decimal amountPaid, decimal total);

        // Method tambahan
        Task<List<Payment>> GetPaymentsByDateAsync(DateTime date);
        Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Payment>> GetPaymentsByMethodAsync(string paymentMethod);
        Task<bool> ValidatePaymentAsync(PaymentViewModel paymentModel);
        Task<Payment> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason = "");
        Task<Dictionary<string, decimal>> GetPaymentSummaryAsync(DateTime date);
        Task<bool> IsTableAvailableAsync(int tableNo);
        Task<List<int>> GetOccupiedTablesAsync();
        decimal CalculateTip(decimal total, decimal tipPercentage);
        Task<Payment> UpdatePaymentStatusAsync(int paymentId, string status);

        // Helper methods
        Task<bool> ProcessCashPaymentAsync(decimal amount, decimal total);
        Task<bool> ProcessCardPaymentAsync(decimal amount, string cardNumber = "");
        Task<List<Payment>> GetTodayPaymentsAsync();
        Task<decimal> GetTodayTotalAsync();
        string GenerateReceiptNumber();
    }
}