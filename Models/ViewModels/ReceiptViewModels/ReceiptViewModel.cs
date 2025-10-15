namespace POSRestoran01.Models.ViewModels.ReceiptViewModels
{
    public class ReceiptViewModel
    {
    
        public string RestaurantName { get; set; } = string.Empty;
        public string RestaurantAddress { get; set; } = string.Empty;
        public string RestaurantPhone { get; set; } = string.Empty;

        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public TimeSpan OrderTime { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public int? TableNo { get; set; }

        public string? CustomerName { get; set; }

        public string CashierName { get; set; } = string.Empty;

        public List<ReceiptItemViewModel> Items { get; set; } = new List<ReceiptItemViewModel>();

        public decimal Subtotal { get; set; }
        public decimal MenuDiscountTotal { get; set; }
        public decimal OrderDiscount { get; set; }
        public decimal TotalBeforeTax { get; set; }
        public decimal PPN { get; set; }
        public decimal PPNPercentage { get; set; } = 11m;
        public decimal Total { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }

        public decimal TotalSavings { get; set; }
        public bool HasDiscount => MenuDiscountTotal > 0 || OrderDiscount > 0;

        public DateTime PrintedAt { get; set; } = DateTime.Now;
        public string ReceiptFooter { get; set; } = "Terima Kasih Atas Kunjungan Anda!";
    }

    public class ReceiptItemViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string? OrderNote { get; set; }

        public bool HasDiscount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalSavingsPerItem { get; set; }

        public string FormattedDiscountPercentage => HasDiscount ? $"-{DiscountPercentage:0}%" : "";
    }
}