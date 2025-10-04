using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSRestoran01.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [Display(Name = "Order ID")]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Metode Pembayaran")]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Jumlah Dibayar")]
        [Range(0, double.MaxValue, ErrorMessage = "Jumlah dibayar harus lebih besar dari 0")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Kembalian")]
        public decimal ChangeAmount { get; set; } = 0;

        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}