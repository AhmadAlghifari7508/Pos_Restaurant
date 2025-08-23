using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Models.ViewModels.SettingsViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Controllers
{
    public class SettingsController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IStockHistoryService _stockHistoryService;
        private readonly IUserActivityService _userActivityService;

        public SettingsController(
            IUserService userService,
            IStockHistoryService stockHistoryService,
            IUserActivityService userActivityService)
        {
            _userService = userService;
            _stockHistoryService = stockHistoryService;
            _userActivityService = userActivityService;
        }

        public async Task<IActionResult> Index(string section = "account")
        {
            try
            {
                var model = new SettingsViewModel
                {
                    ActiveSection = section
                };

                switch (section.ToLower())
                {
                    case "account":
                        model.Users = await _userService.GetAllUsersAsync();
                        break;
                    case "stock-history":
                        model.StockHistories = await _stockHistoryService.GetRecentStockHistoryAsync(50);
                        break;
                    case "user-activity":
                        model.UserActivities = await _userActivityService.GetRecentActivitiesAsync(100);
                        break;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Terjadi kesalahan saat memuat halaman settings.";
                return View(new SettingsViewModel { ActiveSection = section });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return PartialView("_UserManagementPartial", users);
            }
            catch (Exception)
            {
                return BadRequest("Terjadi kesalahan saat memuat data pengguna");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockHistory(DateTime? startDate, DateTime? endDate, int? menuItemId)
        {
            try
            {
                var stockHistories = await _stockHistoryService.GetStockHistoryAsync(startDate, endDate, menuItemId);
                return PartialView("_StockHistoryPartial", stockHistories);
            }
            catch (Exception)
            {
                return BadRequest("Terjadi kesalahan saat memuat riwayat stok");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserActivity(DateTime? startDate, DateTime? endDate, int? userId)
        {
            try
            {
                var activities = await _userActivityService.GetUserActivitiesAsync(startDate, endDate, userId);
                return PartialView("_UserActivityPartial", activities);
            }
            catch (Exception)
            {
                return BadRequest("Terjadi kesalahan saat memuat aktivitas pengguna");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Pengguna tidak ditemukan" });
                }

                return Json(new { 
                    success = true, 
                    data = new {
                        Id = user.Id,
                        FullName = user.FullName,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        IsActive = user.IsActive,
                        LastLogin = user.LastLogin,
                        CreatedAt = user.CreatedAt
                    }
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat memuat data pengguna" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var existingUser = await _userService.GetUserByUsernameAsync(model.Username);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Username sudah digunakan" });
                }

                var existingEmail = await _userService.GetUserByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    return Json(new { success = false, message = "Email sudah digunakan" });
                }

                await _userService.CreateUserAsync(model);

                return Json(new { success = true, message = "Pengguna berhasil ditambahkan" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(UpdateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => x.Value.Errors.First().ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var user = await _userService.GetUserByIdAsync(model.Id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Pengguna tidak ditemukan" });
                }

                // Check if username is taken by another user
                var existingUser = await _userService.GetUserByUsernameAsync(model.Username);
                if (existingUser != null && existingUser.Id != model.Id)
                {
                    return Json(new { success = false, message = "Username sudah digunakan" });
                }

                // Check if email is taken by another user
                var existingEmail = await _userService.GetUserByEmailAsync(model.Email);
                if (existingEmail != null && existingEmail.Id != model.Id)
                {
                    return Json(new { success = false, message = "Email sudah digunakan" });
                }

                await _userService.UpdateUserAsync(model);

                return Json(new { success = true, message = "Pengguna berhasil diperbarui" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Pengguna tidak ditemukan" });
                }

                // Don't allow disabling current user
                if (user.Id == GetCurrentUserId())
                {
                    return Json(new { success = false, message = "Tidak dapat menonaktifkan akun sendiri" });
                }

                user.IsActive = !user.IsActive;
                await _userService.UpdateUserAsync(new UpdateUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    IsActive = user.IsActive
                });

                var status = user.IsActive ? "diaktifkan" : "dinonaktifkan";
                return Json(new { success = true, message = $"Pengguna berhasil {status}" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat mengubah status pengguna" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                // Don't allow deleting current user
                if (userId == GetCurrentUserId())
                {
                    return Json(new { success = false, message = "Tidak dapat menghapus akun sendiri" });
                }

                var success = await _userService.DeleteUserAsync(userId);
                if (success)
                {
                    return Json(new { success = true, message = "Pengguna berhasil dihapus" });
                }
                return Json(new { success = false, message = "Pengguna tidak ditemukan" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus pengguna" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Record logout activity
                await _userActivityService.RecordActivityAsync(
                    GetCurrentUserId(),
                    "Logout",
                    null
                );

                // Clear session
                HttpContext.Session.Clear();

                return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
            }
            catch (Exception)
            {
                // Even if recording activity fails, still logout
                HttpContext.Session.Clear();
                return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
            }
        }
    }
}