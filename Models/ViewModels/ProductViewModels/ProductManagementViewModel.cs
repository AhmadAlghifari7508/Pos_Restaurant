using POSRestoran01.Models;
using System.ComponentModel.DataAnnotations;

namespace POSRestoran01.Models.ViewModels.ProductViewModels
{
    public class ProductManagementViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public int SelectedCategoryId { get; set; }
    }

    public class CreateMenuItemViewModel
    {
        [Required(ErrorMessage = "Kategori harus dipilih")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Nama item tidak boleh kosong")]
        [StringLength(100, ErrorMessage = "Nama item maksimal 100 karakter")]
        [Display(Name = "Nama Item")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Harga tidak boleh kosong")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Harga harus lebih besar dari 0")]
        [Display(Name = "Harga")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stok tidak boleh kosong")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok tidak boleh negatif")]
        [Display(Name = "Stok")]
        public int Stock { get; set; }

        [Display(Name = "Gambar")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Discount Properties
        [Range(0, 100, ErrorMessage = "Persentase diskon harus antara 0 dan 100")]
        [Display(Name = "Persentase Diskon (%)")]
        public decimal? DiscountPercentage { get; set; } = 0;

        [Display(Name = "Tanggal Mulai Diskon")]
        [DataType(DataType.DateTime)]
        public DateTime? DiscountStartDate { get; set; }

        [Display(Name = "Tanggal Berakhir Diskon")]
        [DataType(DataType.DateTime)]
        public DateTime? DiscountEndDate { get; set; }

        [Display(Name = "Aktifkan Diskon")]
        public bool IsDiscountActive { get; set; } = false;
    }

    public class UpdateMenuItemViewModel
    {
        [Required]
        public int MenuItemId { get; set; }

        [Required(ErrorMessage = "Kategori harus dipilih")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Nama item tidak boleh kosong")]
        [StringLength(100, ErrorMessage = "Nama item maksimal 100 karakter")]
        [Display(Name = "Nama Item")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Harga tidak boleh kosong")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Harga harus lebih besar dari 0")]
        [Display(Name = "Harga")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stok tidak boleh kosong")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok tidak boleh negatif")]
        [Display(Name = "Stok")]
        public int Stock { get; set; }

        [Display(Name = "Gambar")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        public string? CurrentImagePath { get; set; }

        // Discount Properties
        [Range(0, 100, ErrorMessage = "Persentase diskon harus antara 0 dan 100")]
        [Display(Name = "Persentase Diskon (%)")]
        public decimal? DiscountPercentage { get; set; } = 0;

        [Display(Name = "Tanggal Mulai Diskon")]
        [DataType(DataType.DateTime)]
        public DateTime? DiscountStartDate { get; set; }

        [Display(Name = "Tanggal Berakhir Diskon")]
        [DataType(DataType.DateTime)]
        public DateTime? DiscountEndDate { get; set; }

        [Display(Name = "Aktifkan Diskon")]
        public bool IsDiscountActive { get; set; } = false;
    }

    public class MenuDiscountViewModel
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }
        public bool IsDiscountActive { get; set; }
        public bool HasActiveDiscount { get; set; }
    }
}