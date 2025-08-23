using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSRestoran01.Models
{
    public class UserActivity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        [Display(Name = "User ID")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tipe Aktivitas")]
        public string ActivityType { get; set; } = string.Empty; // "Login", "Logout", "Create Order", "Update Stock", "Process Payment"

        [Display(Name = "Order ID")]
        public int? OrderId { get; set; } // Nullable, hanya diisi jika aktivitas terkait order

        [Display(Name = "Waktu Aktivitas")]
        public DateTime ActivityTime { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}