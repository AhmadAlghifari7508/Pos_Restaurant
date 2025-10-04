namespace POSRestoran01.Models.ViewModels.HomeViewModels
{
    public class OrderViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal MenuDiscountTotal { get; set; }
        public decimal PPN { get; set; }
        public decimal Total { get; set; }

       
        public decimal TotalSavings => MenuDiscountTotal;
        public decimal TotalDiscountAmount => Discount + MenuDiscountTotal;
    }

    public class OrderItemViewModel
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string? OrderNote { get; set; }

      
        public decimal OriginalPrice { get; set; } = 0;
        public decimal DiscountPercentage { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public bool HasDiscount { get; set; } = false;

        
        public decimal TotalSavingsPerItem => DiscountAmount * Quantity;
        public string FormattedDiscountPercentage => HasDiscount ? $"-{DiscountPercentage:0}%" : "";
    }
}