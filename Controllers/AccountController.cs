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

        public IActionResult Login(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _authService.AuthenticateAsync(model);
                if (user != null)
                {
                    // Set session
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FullName", user.FullName);
                    HttpContext.Session.SetString("Role", user.Role);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Username atau password salah.");
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
