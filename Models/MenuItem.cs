using POSRestoran01.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSRestoran01.Models
{
    public class MenuItem
    {
        [Key]
        public int MenuItemId { get; set; }

        [Required]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nama Item")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Harga")]
        [Range(0, double.MaxValue, ErrorMessage = "Harga harus lebih besar dari 0")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Stok")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok tidak boleh negatif")]
        public int Stock { get; set; } = 0;

        [StringLength(255)]
        [Display(Name = "Path Gambar")]
        public string? ImagePath { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Diperbarui Pada")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}