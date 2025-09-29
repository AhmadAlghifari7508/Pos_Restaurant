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

        // Discount Properties
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Persentase Diskon")]
        [Range(0, 100, ErrorMessage = "Persentase diskon harus antara 0 dan 100")]
        public decimal? DiscountPercentage { get; set; } = 0.00m;

        [Display(Name = "Tanggal Mulai Diskon")]
        public DateTime? DiscountStartDate { get; set; }

        [Display(Name = "Tanggal Berakhir Diskon")]
        public DateTime? DiscountEndDate { get; set; }

        [Display(Name = "Status Diskon Aktif")]
        public bool IsDiscountActive { get; set; } = false;

        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Diperbarui Pada")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // Computed Properties
        [NotMapped]
        public bool HasActiveDiscount => IsDiscountActive &&
            DiscountPercentage > 0 &&
            (!DiscountStartDate.HasValue || DiscountStartDate <= DateTime.Now) &&
            (!DiscountEndDate.HasValue || DiscountEndDate >= DateTime.Now);

        [NotMapped]
        [Display(Name = "Harga Setelah Diskon")]
        public decimal FinalPrice => HasActiveDiscount && DiscountPercentage.HasValue
            ? Price - (Price * (DiscountPercentage.Value / 100))
            : Price;

        [NotMapped]
        [Display(Name = "Jumlah Diskon")]
        public decimal DiscountAmount => HasActiveDiscount && DiscountPercentage.HasValue
            ? Price * (DiscountPercentage.Value / 100)
            : 0;

        // Helper methods
        public bool IsDiscountValidForDate(DateTime? checkDate = null)
        {
            var dateToCheck = checkDate ?? DateTime.Now;

            if (!IsDiscountActive || !DiscountPercentage.HasValue || DiscountPercentage <= 0)
                return false;

            // Check start date
            if (DiscountStartDate.HasValue && dateToCheck < DiscountStartDate.Value)
                return false;

            // Check end date
            if (DiscountEndDate.HasValue && dateToCheck > DiscountEndDate.Value)
                return false;

            return true;
        }

        public decimal GetPriceForDate(DateTime? checkDate = null)
        {
            return IsDiscountValidForDate(checkDate) ? FinalPrice : Price;
        }
    }
}