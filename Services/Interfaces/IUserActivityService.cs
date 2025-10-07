using POSRestoran01.Models;

namespace POSRestoran01.Services.Interfaces
{
    public interface IUserActivityService
    {
        Task<List<UserActivity>> GetUserActivitiesAsync(DateTime? startDate, DateTime? endDate, int? userId);
        Task<List<UserActivity>> GetRecentActivitiesAsync(int count);
        Task RecordActivityAsync(int userId, string activityType, int? orderId = null);
        Task RecordLoginAsync(int userId);
        Task RecordLogoutAsync(int userId);
        Task RecordCloseShiftAsync(int userId); 
    }
}