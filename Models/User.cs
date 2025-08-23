using POSRestoran01.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSRestoran01.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Cashier";

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Login Terakhir")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Diperbarui Pada")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}