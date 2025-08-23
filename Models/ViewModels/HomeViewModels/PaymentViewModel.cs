using System.ComponentModel.DataAnnotations;
namespace POSRestoran01.Models.ViewModels.HomeViewModels
{
    public class PaymentViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Cash";
        public string? CustomerName { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Jumlah cash harus lebih besar dari 0")]
        public decimal Cash { get; set; }
        public decimal Change { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal PPN { get; set; }
        public decimal Total { get; set; }
        [Required]
        public string OrderType { get; set; } = "Dine In";
        public int? TableNo { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
    }
}