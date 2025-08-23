using POSRestoran01.Models;

namespace POSRestoran01.Services.Interfaces
{
    public interface IStockHistoryService
    {
        Task<List<StockHistory>> GetStockHistoryAsync(DateTime? startDate, DateTime? endDate, int? menuItemId);
        Task<List<StockHistory>> GetRecentStockHistoryAsync(int count);
        Task RecordStockChangeAsync(int menuItemId, int userId, int previousStock, int newStock, string changeType, string? notes = null);
    }
}