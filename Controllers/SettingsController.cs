using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using POSRestoran01.Data;
using POSRestoran01.Models.ViewModels.SettingsViewModels;
using POSRestoran01.Services.Interfaces;
using System.Net.Mail;

namespace POSRestoran01.Controllers
{
    public class SettingsController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IStockHistoryService _stockHistoryService;
        private readonly IUserActivityService _userActivityService;
        private readonly IOrderService _orderService;
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context; 

        public SettingsController(
            IUserService userService,
            IStockHistoryService stockHistoryService,
            IUserActivityService userActivityService,
            IOrderService orderService,
            IAuthService authService,
            ApplicationDbContext context)
        {
            _userService = userService;
            _stockHistoryService = stockHistoryService;
            _userActivityService = userActivityService;
            _orderService = orderService;
            _authService = authService;
            _context = context; 
        }

        public async Task<IActionResult> Index(string section = "account")
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                // GUNAKAN AuthService untuk mendapatkan current user
                var currentUser = await _authService.GetUserByIdAsync(currentUserId);

                var model = new SettingsViewModel
                {
                    ActiveSection = section,
                    CurrentUser = currentUser ?? new Models.User()
                };

                // Set ViewBag data for current user - PASTIKAN DATA ADA
                ViewBag.CurrentUserFullName = currentUser?.FullName ?? "User Tidak Ditemukan";
                ViewBag.CurrentUserRole = currentUser?.Role ?? "Role Tidak Ditemukan";
                ViewBag.UserId = currentUserId.ToString();
                ViewBag.CurrentUser = currentUser?.Username ?? "Username Tidak Ditemukan";

                switch (section.ToLower())
                {
                    case "account":
                        // Untuk account section, hanya load current user data (sudah ada di model.CurrentUser)
                        break;
                    case "stock-history":
                        model.StockHistories = await _stockHistoryService.GetRecentStockHistoryAsync(50);
                        break;
                    case "user-activity":
                        model.UserActivities = await _userActivityService.GetRecentActivitiesAsync(100);
                        break;
                    case "cashier-dashboard":
                        model.CashierDashboard = await GetCashierDashboardDataAsync(currentUserId);
                        break;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // Log error untuk debugging
                Console.WriteLine($"Error in Settings Index: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["Error"] = "Terjadi kesalahan saat memuat halaman settings.";

                // Return model dengan data minimal
                var fallbackModel = new SettingsViewModel { ActiveSection = section };
                ViewBag.CurrentUserFullName = "User Tidak Ditemukan";
                ViewBag.CurrentUserRole = "Role Tidak Ditemukan";
                ViewBag.UserId = "0";
                ViewBag.CurrentUser = "Username Tidak Ditemukan";

                return View(fallbackModel);
            }
        }

        #region Current User Account Management - GUNAKAN AuthService

        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return Json(new { success = false, message = "User tidak valid - tidak ada session" });
                }

                // GUNAKAN AuthService
                var user = await _authService.GetUserByIdAsync(currentUserId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan di database" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        fullName = user.FullName,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role,
                        isActive = user.IsActive,
                        lastLogin = user.LastLogin?.ToString("yyyy-MM-dd HH:mm:ss"),
                        createdAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") // TAMBAH CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCurrentUser(string fullName, string email, string currentPassword = null, string newPassword = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Json(new { success = false, message = "User tidak valid" });
                }

                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Nama lengkap dan email harus diisi" });
                }

                // Jika ada password baru, validate password lama
                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (string.IsNullOrEmpty(currentPassword))
                    {
                        return Json(new { success = false, message = "Password lama harus diisi untuk mengubah password" });
                    }

                    if (newPassword.Length < 6)
                    {
                        return Json(new { success = false, message = "Password baru minimal 6 karakter" });
                    }

                    // Validasi password lama menggunakan AuthService
                    var isValidPassword = await _authService.ValidateCurrentPasswordAsync(currentUserId, currentPassword);
                    if (!isValidPassword)
                    {
                        return Json(new { success = false, message = "Password lama tidak valid" });
                    }
                }

                // GUNAKAN AuthService untuk update profile
                var success = await _authService.UpdateUserProfileAsync(currentUserId, fullName, email, newPassword);

                if (success)
                {
                    // Update session dengan data terbaru
                    HttpContext.Session.SetString("FullName", fullName);

                    return Json(new { success = true, message = "Profile berhasil diupdate" });
                }
                else
                {
                    return Json(new { success = false, message = "Gagal mengupdate profile" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion


        #region Create New User - GUNAKAN AuthService

        // HAPUS method CreateUser dengan CreateUserViewModel yang ada di bawah ini
        // Pastikan hanya ada SATU method CreateUser dengan parameter string

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string fullName, string username, string email, string password, string role = "Cashier")
        {
            try
            {
                // Enhanced logging untuk debugging
                Console.WriteLine("=== CreateUser Method Called ===");
                Console.WriteLine($"FullName: '{fullName}' (Length: {fullName?.Length ?? 0})");
                Console.WriteLine($"Username: '{username}' (Length: {username?.Length ?? 0})");
                Console.WriteLine($"Email: '{email}' (Length: {email?.Length ?? 0})");
                Console.WriteLine($"Password: [HIDDEN] (Length: {password?.Length ?? 0})");
                Console.WriteLine($"Role: '{role}'");
                Console.WriteLine($"Current User ID: {GetCurrentUserId()}");

                // Test database connection
                try
                {
                    var userCount = await _context.Users.CountAsync();
                    Console.WriteLine($"Database connection test: SUCCESS - Found {userCount} users in database");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database connection test FAILED: {dbEx.Message}");
                    return Json(new { success = false, message = $"Database connection error: {dbEx.Message}" });
                }

                // Validation dengan logging detail
                if (string.IsNullOrEmpty(fullName) || string.IsNullOrWhiteSpace(fullName))
                {
                    Console.WriteLine("Validation failed: FullName is empty");
                    return Json(new { success = false, message = "Nama lengkap tidak boleh kosong" });
                }

                if (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Validation failed: Username is empty");
                    return Json(new { success = false, message = "Username tidak boleh kosong" });
                }

                if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(email))
                {
                    Console.WriteLine("Validation failed: Email is empty");
                    return Json(new { success = false, message = "Email tidak boleh kosong" });
                }

                if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine("Validation failed: Password is empty");
                    return Json(new { success = false, message = "Password tidak boleh kosong" });
                }

                if (password.Length < 6)
                {
                    Console.WriteLine($"Validation failed: Password too short ({password.Length} characters)");
                    return Json(new { success = false, message = "Password minimal 6 karakter" });
                }

                // Email validation
                if (!IsValidEmail(email))
                {
                    Console.WriteLine($"Validation failed: Invalid email format: {email}");
                    return Json(new { success = false, message = "Format email tidak valid" });
                }

                Console.WriteLine("All validations passed, calling AuthService.CreateUserAsync...");

                // Check if AuthService is null
                if (_authService == null)
                {
                    Console.WriteLine("ERROR: AuthService is null!");
                    return Json(new { success = false, message = "Service tidak tersedia (AuthService null)" });
                }

                // Check if context is null
                if (_context == null)
                {
                    Console.WriteLine("ERROR: ApplicationDbContext is null!");
                    return Json(new { success = false, message = "Database context tidak tersedia" });
                }

                Console.WriteLine("Dependencies check passed, creating user...");

                // GUNAKAN AuthService langsung - method ini sudah handle checking duplicate
                var user = await _authService.CreateUserAsync(fullName, username, email, password, role);

                Console.WriteLine($"User created successfully with ID: {user.Id}");
                Console.WriteLine($"User details - Name: {user.FullName}, Username: {user.Username}, Role: {user.Role}");

                return Json(new { success = true, message = "Pengguna berhasil dibuat" });
            }
            catch (InvalidOperationException ex)
            {
                // Ini akan catch error dari AuthService untuk username/email duplicate
                Console.WriteLine($"Business logic error in CreateUser: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // Catch argument validation errors
                Console.WriteLine($"Argument error in CreateUser: {ex.Message}");
                return Json(new { success = false, message = $"Data tidak valid: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Catch semua error lainnya
                Console.WriteLine($"Unexpected error in CreateUser: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Check inner exception
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                return Json(new { success = false, message = $"Terjadi kesalahan pada sistem: {ex.Message}" });
            }
        }

        // Helper method untuk validasi email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

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
        public async Task<IActionResult> GetCashierDashboard(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var dashboardData = await GetCashierDashboardDataAsync(currentUserId, startDate, endDate);

                return PartialView("_CashierDashboardPartial", dashboardData);
            }
            catch (Exception ex)
            {
                return BadRequest("Terjadi kesalahan saat memuat data dashboard kasir: " + ex.Message);
            }
        }

        private async Task<CashierDashboardViewModel> GetCashierDashboardDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Set default dates dengan benar
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;

            // GUNAKAN AuthService untuk mendapatkan user
            var user = await _authService.GetUserByIdAsync(userId);

            // Load activities dengan range yang lebih lebar untuk recent activities
            var activities = await _userActivityService.GetUserActivitiesAsync(
                startDate ?? DateTime.Today.AddDays(-30), // 30 hari terakhir jika tidak ada filter
                endDate ?? DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59),
                userId
            );

            var userOrders = await _orderService.GetOrdersByUserIdAsync(userId, start, end);
            var todayOrders = await _orderService.GetOrdersByUserIdAsync(userId, DateTime.Today, DateTime.Today);

            // Get last login dan logout dari semua activities
            var allUserActivities = await _userActivityService.GetUserActivitiesAsync(null, null, userId);

            var lastLogin = allUserActivities.Where(a => a.ActivityType == "Login")
                                           .OrderByDescending(a => a.ActivityTime)
                                           .FirstOrDefault()?.ActivityTime;

            var lastLogout = allUserActivities.Where(a => a.ActivityType == "Logout")
                                            .OrderByDescending(a => a.ActivityTime)
                                            .FirstOrDefault()?.ActivityTime;

            // Calculate working hours berdasarkan login/logout hari ini
            TimeSpan? workingHours = null;
            var todayActivities = allUserActivities.Where(a => a.ActivityTime.Date == DateTime.Today).ToList();

            var todayLogin = todayActivities.Where(a => a.ActivityType == "Login")
                                          .OrderBy(a => a.ActivityTime)
                                          .FirstOrDefault();

            var todayLogout = todayActivities.Where(a => a.ActivityType == "Logout")
                                           .OrderByDescending(a => a.ActivityTime)
                                           .FirstOrDefault();

            if (todayLogin != null)
            {
                var endTime = todayLogout?.ActivityTime ?? DateTime.Now;
                workingHours = endTime - todayLogin.ActivityTime;
            }

            // Calculate statistics
            var statistics = new CashierStatisticsViewModel
            {
                // Period statistics (berdasarkan filter tanggal)
                TotalRevenue = await _orderService.GetTotalRevenueByUserIdAsync(userId, start, end),
                TotalOrders = userOrders.Count(o => o.Status == "Completed"),
                TotalCustomers = await _orderService.GetTotalCustomersByUserIdAsync(userId, start, end),
                TotalMenusOrdered = await _orderService.GetTotalMenusOrderedByUserIdAsync(userId, start, end),

                // Today's statistics
                TodayRevenue = todayOrders.Where(o => o.Status == "Completed").Sum(o => o.Total),
                TodayOrders = todayOrders.Count(o => o.Status == "Completed"),
                TodayCustomers = todayOrders.Count(o => o.Status == "Completed"),
                TodayMenusOrdered = todayOrders.Where(o => o.Status == "Completed")
                                              .SelectMany(o => o.OrderDetails)
                                              .Sum(od => od.Quantity),

                // Activity info
                LastLogin = lastLogin,
                LastLogout = lastLogout,
                WorkingHours = workingHours
            };

            // Convert activities to detailed view model - ambil 20 aktivitas terbaru
            var detailedActivities = activities.Take(20).Select(a => new UserActivityDetailViewModel
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                OrderNumber = a.Order?.OrderNumber,
                ActivityTime = a.ActivityTime,
                FormattedActivityTime = a.ActivityTime.ToString("dd/MM/yyyy HH:mm:ss"),
                OrderTotal = a.Order?.Total,
                CustomerName = a.Order?.CustomerName,
                OrderType = a.Order?.OrderType
            }).ToList();

            return new CashierDashboardViewModel
            {
                CurrentUser = user ?? new Models.User(),
                RecentActivities = detailedActivities,
                TodayOrders = todayOrders,
                Statistics = statistics,
                StartDate = startDate,
                EndDate = endDate
            };
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

                return Json(new
                {
                    success = true,
                    data = new
                    {
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
                var currentUserId = GetCurrentUserId();

                // Record logout activity menggunakan AuthService
                await _authService.LogoutAsync(currentUserId);

                // Clear session
                HttpContext.Session.Clear();

                return Json(new { success = true, redirectUrl = "/Account/Login" });
            }
            catch (Exception ex)
            {
                // Even if recording activity fails, still logout
                HttpContext.Session.Clear();
                return Json(new { success = true, redirectUrl = "/Account/Login" });
            }
        }
    }
}