using Microsoft.EntityFrameworkCore;
using POSRestoran01.Models;

namespace POSRestoran01.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<StockHistory> StockHistories { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.Discount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.PPN)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.Total)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.Subtotal)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.AmountPaid)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.ChangeAmount)
                .HasColumnType("decimal(10,2)");

            // Configure relationships
            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.CategoryId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.MenuItem)
                .WithMany(m => m.OrderDetails)
                .HasForeignKey(od => od.MenuItemId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId);
        }
    }
}
