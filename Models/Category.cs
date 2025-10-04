using System.ComponentModel.DataAnnotations;

namespace POSRestoran01.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Nama Kategori")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Dibuat Pada")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Diperbarui Pada")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

            
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}