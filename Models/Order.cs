using POSRestoran01.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace POSRestoran01.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Nomor Order")]
        public string OrderNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Nama Customer")]
        public string? CustomerName { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Tipe Order")]
        public string OrderType { get; set; } = string.Empty; // 'Dine In', 'Take Away'

        [Display(Name = "Nomor Meja")]
        public int? TableNo { get; set; }

        [Required]
        [Display(Name = "Tanggal Order")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Waktu Order")]
        [DataType(DataType.Time)]
        public TimeSpan OrderTime { get; set; } = DateTime.Now.TimeOfDay;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Diskon Order")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total Diskon Menu")]
        public decimal MenuDiscountTotal { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "PPN (11%)")]
        public decimal PPN { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total")]
        public decimal Total { get; set; } = 0;

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Pending, Preparing, Completed, Canceled

        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Diperbarui Pada")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "User ID")]
        public int UserId { get; set; }

        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        
        [NotMapped]
        [Display(Name = "Total Penghematan")]
        public decimal TotalSavings => MenuDiscountTotal;

        [NotMapped]
        [Display(Name = "Total Diskon Keseluruhan")]
        public decimal TotalDiscountAmount => Discount + MenuDiscountTotal;
    }
}