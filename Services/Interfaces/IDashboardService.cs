using POSRestoran01.Models.ViewModels.DashboardViewModels;

namespace POSRestoran01.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(DateTime date);
        Task<List<OrderReportViewModel>> GetOrderReportsAsync(DateTime? startDate, DateTime? endDate, string? status, int page, int pageSize);
        Task<List<PopularMenuViewModel>> GetPopularMenusAsync(DateTime? startDate, DateTime? endDate);
        Task<List<OrderTypeStatsViewModel>> GetOrderTypeStatsAsync(DateTime? startDate, DateTime? endDate);
        Task<DashboardStatsViewModel> GetDashboardStatsAsync(DateTime date);
    }
}