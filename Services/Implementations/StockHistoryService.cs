using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class StockHistoryService : IStockHistoryService
    {
        private readonly ApplicationDbContext _context;

        public StockHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockHistory>> GetStockHistoryAsync(DateTime? startDate, DateTime? endDate, int? menuItemId)
        {
            var query = _context.StockHistories
                .Include(sh => sh.MenuItem)
                .ThenInclude(m => m.Category)
                .Include(sh => sh.User)
                .AsQueryable();

 
            if (startDate.HasValue)
            {
             
                var start = startDate.Value.Date;
                query = query.Where(sh => sh.ChangedAt >= start);
            }

            if (endDate.HasValue)
            {
  
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(sh => sh.ChangedAt <= end);
            }

            if (menuItemId.HasValue)
            {
                query = query.Where(sh => sh.MenuItemId == menuItemId.Value);
            }

            return await query
                .OrderByDescending(sh => sh.ChangedAt)
                .ToListAsync();
        }

        public async Task<List<StockHistory>> GetRecentStockHistoryAsync(int count)
        {
            return await _context.StockHistories
                .Include(sh => sh.MenuItem)
                .ThenInclude(m => m.Category)
                .Include(sh => sh.User)
                .OrderByDescending(sh => sh.ChangedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task RecordStockChangeAsync(int menuItemId, int userId, int previousStock, int newStock, string changeType, string? notes = null)
        {
            var stockChange = newStock - previousStock;

            var stockHistory = new StockHistory
            {
                MenuItemId = menuItemId,
                UserId = userId,
                PreviousStock = previousStock,
                NewStock = newStock,
                StockChange = stockChange,
                ChangeType = changeType,
                Notes = notes,
                ChangedAt = DateTime.Now
            };

            _context.StockHistories.Add(stockHistory);
            await _context.SaveChangesAsync();
        }
    }
}