namespace POSRestoran01.Models.ViewModels.HomeViewModels
{
    public class POSViewModel
    {
        public string RestaurantName { get; set; } = string.Empty;
        public DateTime CurrentDate { get; set; } = DateTime.Now;
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public int SelectedCategoryId { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public OrderViewModel CurrentOrder { get; set; } = new OrderViewModel();
    }
}