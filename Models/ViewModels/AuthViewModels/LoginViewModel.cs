using System.ComponentModel.DataAnnotations;

namespace POSRestoran01.Models.ViewModels.AuthViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}