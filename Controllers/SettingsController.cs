using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSRestoran01.Data;
using POSRestoran01.Models.ViewModels.SettingsViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Controllers
{
    public class SettingsController : BaseController
    {
        private readonly IStockHistoryService _stockHistoryService;
        private readonly IUserActivityService _userActivityService;
        private readonly IOrderService _orderService;
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public SettingsController(
            IStockHistoryService stockHistoryService,
            IUserActivityService userActivityService,
            IOrderService orderService,
            IAuthService authService,
            ApplicationDbContext context)
        {
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
                var currentUser = await _authService.GetUserByIdAsync(currentUserId);

                var model = new SettingsViewModel
                {
                    ActiveSection = section,
                    CurrentUser = currentUser ?? new Models.User()
                };

                ViewBag.CurrentUserFullName = currentUser?.FullName ?? "User Tidak Ditemukan";
                ViewBag.CurrentUserRole = currentUser?.Role ?? "Role Tidak Ditemukan";
                ViewBag.UserId = currentUserId.ToString();
                ViewBag.CurrentUser = currentUser?.Username ?? "Username Tidak Ditemukan";

                switch (section.ToLower())
                {
                    case "account":
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
                Console.WriteLine($"Error in Settings Index: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["Error"] = "Terjadi kesalahan saat memuat halaman settings.";

                var fallbackModel = new SettingsViewModel { ActiveSection = section };
                ViewBag.CurrentUserFullName = "User Tidak Ditemukan";
                ViewBag.CurrentUserRole = "Role Tidak Ditemukan";
                ViewBag.UserId = "0";
                ViewBag.CurrentUser = "Username Tidak Ditemukan";

                return View(fallbackModel);
            }
        }

        #region Current User Account Management

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
                        createdAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
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

                    var isValidPassword = await _authService.ValidateCurrentPasswordAsync(currentUserId, currentPassword);
                    if (!isValidPassword)
                    {
                        return Json(new { success = false, message = "Password lama tidak valid" });
                    }
                }

                var success = await _authService.UpdateUserProfileAsync(currentUserId, fullName, email, newPassword);

                if (success)
                {
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

        #region Create New User

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string fullName, string username, string email, string password, string role = "Cashier")
        {
            try
            {
                Console.WriteLine("=== CreateUser Method Called ===");
                Console.WriteLine($"FullName: '{fullName}' (Length: {fullName?.Length ?? 0})");
                Console.WriteLine($"Username: '{username}' (Length: {username?.Length ?? 0})");
                Console.WriteLine($"Email: '{email}' (Length: {email?.Length ?? 0})");
                Console.WriteLine($"Password: [HIDDEN] (Length: {password?.Length ?? 0})");
                Console.WriteLine($"Role: '{role}'");
                Console.WriteLine($"Current User ID: {GetCurrentUserId()}");

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

                if (!IsValidEmail(email))
                {
                    Console.WriteLine($"Validation failed: Invalid email format: {email}");
                    return Json(new { success = false, message = "Format email tidak valid" });
                }

                Console.WriteLine("All validations passed, calling AuthService.CreateUserAsync...");

                if (_authService == null)
                {
                    Console.WriteLine("ERROR: AuthService is null!");
                    return Json(new { success = false, message = "Service tidak tersedia (AuthService null)" });
                }

                if (_context == null)
                {
                    Console.WriteLine("ERROR: ApplicationDbContext is null!");
                    return Json(new { success = false, message = "Database context tidak tersedia" });
                }

                Console.WriteLine("Dependencies check passed, creating user...");

                var user = await _authService.CreateUserAsync(fullName, username, email, password, role);

                Console.WriteLine($"User created successfully with ID: {user.Id}");
                Console.WriteLine($"User details - Name: {user.FullName}, Username: {user.Username}, Role: {user.Role}");

                return Json(new { success = true, message = "Pengguna berhasil dibuat" });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Business logic error in CreateUser: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error in CreateUser: {ex.Message}");
                return Json(new { success = false, message = $"Data tidak valid: {ex.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in CreateUser: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                return Json(new { success = false, message = $"Terjadi kesalahan pada sistem: {ex.Message}" });
            }
        }

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
        public async Task<IActionResult> GetCashierDashboard(DateTime? selectedDate)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var dashboardData = await GetCashierDashboardDataAsync(currentUserId, selectedDate);

                return PartialView("_CashierDashboardPartial", dashboardData);
            }
            catch (Exception ex)
            {
                return BadRequest("Terjadi kesalahan saat memuat data dashboard kasir: " + ex.Message);
            }
        }

        private async Task<CashierDashboardViewModel> GetCashierDashboardDataAsync(int userId, DateTime? selectedDate = null)
        {
            var targetDate = selectedDate ?? DateTime.Today;
            var startOfDay = targetDate.Date;
            var endOfDay = targetDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var user = await _authService.GetUserByIdAsync(userId);

            var activities = await _userActivityService.GetUserActivitiesAsync(
                startOfDay,
                endOfDay,
                userId
            );

            var dateOrders = await _orderService.GetOrdersByUserIdAsync(userId, startOfDay, endOfDay);

            var todayOrders = await _orderService.GetOrdersByUserIdAsync(userId, DateTime.Today, DateTime.Today);

            var firstLoginOfDay = activities.Where(a => a.ActivityType == "Login")
                                           .OrderBy(a => a.ActivityTime)
                                           .FirstOrDefault()?.ActivityTime;

            var lastLogoutOfDay = activities.Where(a => a.ActivityType == "Logout")
                                           .OrderByDescending(a => a.ActivityTime)
                                           .FirstOrDefault()?.ActivityTime;

            TimeSpan? workingHours = null;
            var selectedDayActivities = activities.ToList();

            var firstLogin = selectedDayActivities.Where(a => a.ActivityType == "Login")
                                                .OrderBy(a => a.ActivityTime)
                                                .FirstOrDefault();

            var lastLogout = selectedDayActivities.Where(a => a.ActivityType == "Logout")
                                                .OrderByDescending(a => a.ActivityTime)
                                                .FirstOrDefault();

            if (firstLogin != null && lastLogout != null)
            {
                workingHours = lastLogout.ActivityTime - firstLogin.ActivityTime;
            }

            var statistics = new CashierStatisticsViewModel
            {
                TotalRevenue = await _orderService.GetTotalRevenueByUserIdAsync(userId, startOfDay, endOfDay),
                TotalOrders = dateOrders.Count(o => o.Status == "Completed"),
                TotalCustomers = await _orderService.GetTotalCustomersByUserIdAsync(userId, startOfDay, endOfDay),
                TotalMenusOrdered = await _orderService.GetTotalMenusOrderedByUserIdAsync(userId, startOfDay, endOfDay),

                TodayRevenue = todayOrders.Where(o => o.Status == "Completed").Sum(o => o.Total),
                TodayOrders = todayOrders.Count(o => o.Status == "Completed"),
                TodayCustomers = todayOrders.Count(o => o.Status == "Completed"),
                TodayMenusOrdered = todayOrders.Where(o => o.Status == "Completed")
                                              .SelectMany(o => o.OrderDetails)
                                              .Sum(od => od.Quantity),

                LastLogin = firstLoginOfDay,
                LastLogout = lastLogoutOfDay,
                WorkingHours = workingHours
            };

            var detailedActivities = activities.Select(a => new UserActivityDetailViewModel
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
                TodayOrders = dateOrders,
                Statistics = statistics,
                StartDate = selectedDate,
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                await _authService.LogoutAsync(currentUserId);

                HttpContext.Session.Clear();

                return Json(new { success = true, redirectUrl = "/Account/Login" });
            }
            catch (Exception ex)
            {
                HttpContext.Session.Clear();
                return Json(new { success = true, redirectUrl = "/Account/Login" });
            }
        }
    }
}