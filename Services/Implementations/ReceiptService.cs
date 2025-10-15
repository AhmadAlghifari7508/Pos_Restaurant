using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models.ViewModels.ReceiptViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class ReceiptService : IReceiptService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ReceiptService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ReceiptViewModel?> GenerateReceiptAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return null;

            var (restaurantName, address, phone) = GetRestaurantInfo();
            var payment = order.Payments.FirstOrDefault();

            var receipt = new ReceiptViewModel
            {
      
                RestaurantName = restaurantName,
                RestaurantAddress = address,
                RestaurantPhone = phone,

                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                OrderTime = order.OrderTime,
                OrderType = order.OrderType,
                TableNo = order.TableNo,

                CustomerName = order.CustomerName,

                CashierName = order.User?.FullName ?? "Unknown",

                Items = order.OrderDetails.Select(od => new ReceiptItemViewModel
                {
                    ItemName = od.MenuItem?.ItemName ?? "Unknown Item",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    OriginalPrice = od.OriginalPrice > 0 ? od.OriginalPrice : od.UnitPrice,
                    Subtotal = od.Subtotal,
                    OrderNote = od.OrderNote,
                    HasDiscount = od.HasDiscount,
                    DiscountPercentage = od.DiscountPercentage,
                    DiscountAmount = od.DiscountAmount,
                    TotalSavingsPerItem = od.DiscountAmount * od.Quantity
                }).ToList(),

                Subtotal = order.Subtotal,
                MenuDiscountTotal = order.MenuDiscountTotal,
                OrderDiscount = order.Discount,
                TotalBeforeTax = order.Subtotal - order.Discount,
                PPN = order.PPN,
                Total = order.Total,

                PaymentMethod = payment?.PaymentMethod ?? "Cash",
                AmountPaid = payment?.AmountPaid ?? 0,
                ChangeAmount = payment?.ChangeAmount ?? 0,


                TotalSavings = order.MenuDiscountTotal + order.Discount,

                PrintedAt = DateTime.Now
            };

            return receipt;
        }

        public async Task<ReceiptViewModel?> GenerateReceiptByOrderNumberAsync(string orderNumber)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
                return null;

            return await GenerateReceiptAsync(order.OrderId);
        }

        public async Task<bool> CanGenerateReceiptAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return false;

            return order.Status == "Completed" && order.Payments.Any();
        }

        public (string restaurantName, string address, string phone) GetRestaurantInfo()
        {
            var restaurantName = _configuration["AppSettings:RestaurantName"] ?? "Restaurant Wisesa";
            var address = _configuration["AppSettings:RestaurantAddress"] ?? "Jl. Ampasit VI No.11A, RT.2/RW.2, Cideng, Kecamatan Gambir, Kota Jakarta Pusat";
            var phone = _configuration["AppSettings:RestaurantPhone"] ?? "088213622030";

            return (restaurantName, address, phone);
        }
    }
}