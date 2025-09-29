using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace POSRestoran01.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        [Required]
        [Display(Name = "Order ID")]
        public int OrderId { get; set; }

        [Required]
        [Display(Name = "Menu Item ID")]
        public int MenuItemId { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih besar dari 0")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Harga Satuan")]
        public decimal UnitPrice { get; set; }

        [StringLength(255)]
        [Display(Name = "Catatan Order")]
        public string? OrderNote { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        // Discount Properties untuk menyimpan info diskon saat order dibuat
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Harga Asli")]
        public decimal OriginalPrice { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Persentase Diskon")]
        public decimal DiscountPercentage { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Jumlah Diskon")]
        public decimal DiscountAmount { get; set; } = 0;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; } = null!;

        // Computed Properties
        [NotMapped]
        public bool HasDiscount => DiscountPercentage > 0 && DiscountAmount > 0;

        [NotMapped]
        [Display(Name = "Total Penghematan Item")]
        public decimal TotalSavings => DiscountAmount * Quantity;
    }
}