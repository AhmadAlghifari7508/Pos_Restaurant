using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Services.Implementations
{
    public class UserActivityService : IUserActivityService
    {
        private readonly ApplicationDbContext _context;

        public UserActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserActivity>> GetUserActivitiesAsync(DateTime? startDate, DateTime? endDate, int? userId)
        {
            var query = _context.UserActivities
                .Include(ua => ua.User)
                .Include(ua => ua.Order)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(ua => ua.ActivityTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(ua => ua.ActivityTime <= endDate.Value);

            if (userId.HasValue)
                query = query.Where(ua => ua.UserId == userId.Value);

            return await query
                .OrderByDescending(ua => ua.ActivityTime)
                .ToListAsync();
        }

        public async Task<List<UserActivity>> GetRecentActivitiesAsync(int count)
        {
            return await _context.UserActivities
                .Include(ua => ua.User)
                .Include(ua => ua.Order)
                .OrderByDescending(ua => ua.ActivityTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task RecordActivityAsync(int userId, string activityType, int? orderId = null)
        {
            var activity = new UserActivity
            {
                UserId = userId,
                ActivityType = activityType,
                OrderId = orderId,
                ActivityTime = DateTime.Now
            };

            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();
        }

        public async Task RecordLoginAsync(int userId)
        {
            await RecordActivityAsync(userId, "Login");
        }

        public async Task RecordLogoutAsync(int userId)
        {
            await RecordActivityAsync(userId, "Logout");
        }
    }
}