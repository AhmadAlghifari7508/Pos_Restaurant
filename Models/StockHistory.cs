using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSRestoran01.Models
{
    public class StockHistory
    {
        [Key]
        public int StockHistoryId { get; set; }

        [Required]
        [Display(Name = "Menu Item ID")]
        public int MenuItemId { get; set; }

        [Required]
        [Display(Name = "User ID")]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Stok Sebelumnya")]
        public int PreviousStock { get; set; }

        [Required]
        [Display(Name = "Stok Baru")]
        public int NewStock { get; set; }

        [Required]
        [Display(Name = "Jumlah Perubahan")]
        public int StockChange { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tipe Perubahan")]
        public string ChangeType { get; set; } = string.Empty; // "Manual Update", "Order Reduction", "Initial Stock"

        [StringLength(500)]
        [Display(Name = "Keterangan")]
        public string? Notes { get; set; }

        [Display(Name = "Tanggal Perubahan")]
        public DateTime ChangedAt { get; set; } = DateTime.Now;


        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}