using Microsoft.AspNetCore.Mvc;
using POSRestoran01.Models.ViewModels.AuthViewModels;
using POSRestoran01.Services.Interfaces;

namespace POSRestoran01.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Jika sudah login, redirect ke home
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _authService.AuthenticateAsync(model);
                    if (user != null)
                    {
                        // Set session
                        HttpContext.Session.SetString("UserId", user.Id.ToString());
                        HttpContext.Session.SetString("Username", user.Username);
                        HttpContext.Session.SetString("FullName", user.FullName);
                        HttpContext.Session.SetString("UserRole", user.Role);

                        // Activity recording sudah dilakukan di AuthService
                        TempData["Success"] = $"Selamat datang, {user.FullName}!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Username atau password salah, atau akun tidak aktif");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Terjadi kesalahan saat login. Silakan coba lagi.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
                {
                    // Record logout activity
                    await _authService.LogoutAsync(userId);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with logout
                // Logging dapat ditambahkan di sini
            }

            HttpContext.Session.Clear();
            TempData["Success"] = "Anda telah berhasil logout.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> LogoutJson()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
                {
                    // Record logout activity
                    await _authService.LogoutAsync(userId);
                }

                HttpContext.Session.Clear();
                return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
            }
            catch (Exception)
            {
                // Even if there's an error, still logout
                HttpContext.Session.Clear();
                return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}