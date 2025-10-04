using POSRestoran01.Models;
using System.ComponentModel.DataAnnotations;

namespace POSRestoran01.Models.ViewModels.SettingsViewModels
{
    public class SettingsViewModel
    {
        public string ActiveSection { get; set; } = "account";
        public List<User> Users { get; set; } = new List<User>();
        public List<StockHistory> StockHistories { get; set; } = new List<StockHistory>();
        public List<UserActivity> UserActivities { get; set; } = new List<UserActivity>();

        
        public CashierDashboardViewModel CashierDashboard { get; set; } = new CashierDashboardViewModel();
        public User CurrentUser { get; set; } = new User();
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Nama lengkap tidak boleh kosong")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username tidak boleh kosong")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email tidak boleh kosong")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password tidak boleh kosong")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password tidak boleh kosong")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok")]
        [Display(Name = "Konfirmasi Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role harus dipilih")]
        [StringLength(20, ErrorMessage = "Role maksimal 20 karakter")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Cashier";

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class UpdateUserViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama lengkap tidak boleh kosong")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username tidak boleh kosong")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email tidak boleh kosong")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [Display(Name = "Password Baru (Kosongkan jika tidak ingin mengubah)")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Password dan konfirmasi password tidak cocok")]
        [Display(Name = "Konfirmasi Password Baru")]
        public string? ConfirmNewPassword { get; set; }

        [Required(ErrorMessage = "Role harus dipilih")]
        [StringLength(20, ErrorMessage = "Role maksimal 20 karakter")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Cashier";

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class StockHistoryViewModel
    {
        public int StockHistoryId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public int StockChange { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class UserActivityViewModel
    {
        public int ActivityId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
        public DateTime ActivityTime { get; set; }
        public string FormattedActivityTime { get; set; } = string.Empty;
    }

    
    public class CashierDashboardViewModel
    {
        public User CurrentUser { get; set; } = new User();
        public List<UserActivityDetailViewModel> RecentActivities { get; set; } = new List<UserActivityDetailViewModel>();
        public List<Order> TodayOrders { get; set; } = new List<Order>();
        public CashierStatisticsViewModel Statistics { get; set; } = new CashierStatisticsViewModel();

        
        public DateTime? StartDate { get; set; }

      
        public string SelectedDateDisplay => StartDate?.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID")) ?? "Hari Ini";
    }

    public class CashierStatisticsViewModel
    {
       
        public decimal TotalRevenue { get; set; }
        public int TotalMenusOrdered { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }

        
        public DateTime? LastLogin { get; set; } 
        public DateTime? LastLogout { get; set; } 
        public TimeSpan? WorkingHours { get; set; }

        
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int TodayCustomers { get; set; }
        public int TodayMenusOrdered { get; set; }
    }

    public class UserActivityDetailViewModel
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
        public DateTime ActivityTime { get; set; }
        public string FormattedActivityTime { get; set; } = string.Empty;
        public decimal? OrderTotal { get; set; }
        public string? CustomerName { get; set; }
        public string? OrderType { get; set; }
    }
}