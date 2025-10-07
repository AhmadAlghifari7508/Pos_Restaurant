using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace POSRestoran01.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }
            ViewBag.CurrentUser = username;
            ViewBag.UserId = userId;
            base.OnActionExecuting(context);
        }


        protected int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return 0;
        }

        protected string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("Username") ?? "Unknown";
        }

        protected string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "Cashier";
        }

        protected bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }
    }
}