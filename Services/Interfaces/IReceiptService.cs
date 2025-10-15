using POSRestoran01.Models.ViewModels.ReceiptViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IReceiptService
    {

        Task<ReceiptViewModel?> GenerateReceiptAsync(int orderId);


        Task<ReceiptViewModel?> GenerateReceiptByOrderNumberAsync(string orderNumber);

        Task<bool> CanGenerateReceiptAsync(int orderId);

        (string restaurantName, string address, string phone) GetRestaurantInfo();
    }
}