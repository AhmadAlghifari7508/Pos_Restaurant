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
        public string ActivityType { get; set; } = string.Empty; 

        [Display(Name = "Order ID")]
        public int? OrderId { get; set; } 

        [Display(Name = "Waktu Aktivitas")]
        public DateTime ActivityTime { get; set; } = DateTime.Now;

    
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}