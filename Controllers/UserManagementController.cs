using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Services.Interfaces;



namespace POSRestoran01.Controllers
{
    public class UserManagementController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public UserManagementController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string fullName, string username, string email, string password, string role = "Cashier")
        {
            try
            {
                await _authService.CreateUserAsync(fullName, username, email, password, role);
                return Json(new { success = true, message = "User berhasil dibuat" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var success = await _authService.ChangePasswordAsync(userId, currentPassword, newPassword);
                return Json(new
                {
                    success = success,
                    message = success ? "Password berhasil diubah" : "Password lama tidak valid"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}