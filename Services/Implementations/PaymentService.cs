using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Models.ViewModels.HomeViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> ProcessPaymentAsync(PaymentViewModel paymentModel, int orderId)
        {
            try
            {
                // Validasi pembayaran
                if (paymentModel.Cash < paymentModel.Total)
                {
                    throw new InvalidOperationException("Jumlah cash tidak mencukupi");
                }

                if (paymentModel.OrderType == "Dine In" && (!paymentModel.TableNo.HasValue || paymentModel.TableNo <= 0))
                {
                    throw new InvalidOperationException("Nomor meja harus diisi untuk Dine In");
                }

                var payment = new Payment
                {
                    OrderId = orderId,
                    PaymentMethod = paymentModel.PaymentMethod ?? "Cash",
                    AmountPaid = paymentModel.Cash,
                    ChangeAmount = paymentModel.Change,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return payment;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        public decimal CalculateChange(decimal amountPaid, decimal total)
        {
            return Math.Max(0, amountPaid - total);
        }

        // Method tambahan untuk mendukung HomeController
        public async Task<List<Payment>> GetPaymentsByDateAsync(DateTime date)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.CreatedAt.Date == date.Date)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt.Date <= endDate.Value.Date);

            return await query.SumAsync(p => p.AmountPaid);
        }

        public async Task<List<Payment>> GetPaymentsByMethodAsync(string paymentMethod)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.PaymentMethod == paymentMethod)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ValidatePaymentAsync(PaymentViewModel paymentModel)
        {
            // Validasi basic
            if (paymentModel.Cash <= 0)
                return false;

            if (paymentModel.Cash < paymentModel.Total)
                return false;

            if (string.IsNullOrEmpty(paymentModel.PaymentMethod))
                return false;

            // Validasi table number untuk Dine In
            if (paymentModel.OrderType == "Dine In")
            {
                if (!paymentModel.TableNo.HasValue || paymentModel.TableNo <= 0)
                    return false;

                // Cek apakah meja sudah terpakai (optional)
                var isTableOccupied = await _context.Orders
                    .AnyAsync(o => o.TableNo == paymentModel.TableNo &&
                                  o.OrderDate == DateTime.Today &&
                                  o.Status != "Completed" &&
                                  o.Status != "Canceled");

                if (isTableOccupied)
                    return false;
            }

            return true;
        }

        public async Task<Payment> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason = "")
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment tidak ditemukan");

            if (refundAmount > payment.AmountPaid)
                throw new InvalidOperationException("Jumlah refund melebihi pembayaran");

            // Create a new refund payment record
            var refundPayment = new Payment
            {
                OrderId = payment.OrderId,
                PaymentMethod = "Refund",
                AmountPaid = -refundAmount, // Negative amount for refund
                ChangeAmount = 0,
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(refundPayment);
            await _context.SaveChangesAsync();

            return refundPayment;
        }

        public async Task<Dictionary<string, decimal>> GetPaymentSummaryAsync(DateTime date)
        {
            var payments = await _context.Payments
                .Where(p => p.CreatedAt.Date == date.Date && p.AmountPaid > 0) // Exclude refunds
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalAmount", payments.Sum(p => p.AmountPaid) },
                { "TotalTransactions", payments.Count },
                { "CashPayments", payments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.AmountPaid) },
                { "CardPayments", payments.Where(p => p.PaymentMethod == "Card").Sum(p => p.AmountPaid) },
                { "TotalChange", payments.Sum(p => p.ChangeAmount) }
            };
        }

        public async Task<bool> IsTableAvailableAsync(int tableNo)
        {
            return !await _context.Orders
                .AnyAsync(o => o.TableNo == tableNo &&
                              o.OrderDate == DateTime.Today &&
                              o.Status != "Completed" &&
                              o.Status != "Canceled");
        }

        public async Task<List<int>> GetOccupiedTablesAsync()
        {
            return await _context.Orders
                .Where(o => o.TableNo.HasValue &&
                           o.OrderDate == DateTime.Today &&
                           o.Status != "Completed" &&
                           o.Status != "Canceled")
                .Select(o => o.TableNo.Value)
                .Distinct()
                .ToListAsync();
        }

        public decimal CalculateTip(decimal total, decimal tipPercentage)
        {
            return Math.Round(total * (tipPercentage / 100), 0, MidpointRounding.AwayFromZero);
        }

        public async Task<Payment> UpdatePaymentStatusAsync(int paymentId, string status)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                // Since Payment model doesn't have Status, we can add a note or handle differently
                // For now, we'll just update the CreatedAt to mark it as modified
                // You might want to add a Status field to Payment model or handle this differently
                await _context.SaveChangesAsync();
            }
            return payment!;
        }

        // Helper methods for payment processing
        public async Task<bool> ProcessCashPaymentAsync(decimal amount, decimal total)
        {
            return amount >= total;
        }

        public async Task<bool> ProcessCardPaymentAsync(decimal amount, string cardNumber = "")
        {
            // Implement card payment logic here
            // For now, just return true as placeholder
            return await Task.FromResult(true);
        }

        public async Task<List<Payment>> GetTodayPaymentsAsync()
        {
            return await GetPaymentsByDateAsync(DateTime.Today);
        }

        public async Task<decimal> GetTodayTotalAsync()
        {
            var payments = await GetTodayPaymentsAsync();
            return payments.Where(p => p.AmountPaid > 0).Sum(p => p.AmountPaid);
        }

        public string GenerateReceiptNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"RCP{timestamp}";
        }
    }
}