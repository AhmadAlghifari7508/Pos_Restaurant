namespace POSRestoran01.Models.ViewModels.HomeViewModels
{
    public class OrderViewModel
    {
        public string OrderNumber { get; set; } = string.Empty;
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal PPN { get; set; }
        public decimal Total { get; set; }
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
    }
}