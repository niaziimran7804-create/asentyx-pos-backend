using Microsoft.EntityFrameworkCore;
using POS.Api.Models;

namespace POS.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<MainCategory> MainCategories { get; set; }
        public DbSet<SecondCategory> SecondCategories { get; set; }
        public DbSet<ThirdCategory> ThirdCategories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProductMap> OrderProductMaps { get; set; }
        public DbSet<BarCode> BarCodes { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<ShopConfiguration> ShopConfigurations { get; set; }
        public DbSet<OrderHistory> OrderHistories { get; set; }
        public DbSet<AccountingEntry> AccountingEntries { get; set; }
        public DbSet<DailySales> DailySales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Brand>()
                .HasOne(b => b.Vendor)
                .WithMany(v => v.Brands)
                .HasForeignKey(b => b.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.ThirdCategory)
                .WithMany(tc => tc.Vendors)
                .HasForeignKey(v => v.ThirdCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SecondCategory>()
                .HasOne(sc => sc.MainCategory)
                .WithMany(mc => mc.SecondCategories)
                .HasForeignKey(sc => sc.MainCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThirdCategory>()
                .HasOne(tc => tc.SecondCategory)
                .WithMany(sc => sc.ThirdCategories)
                .HasForeignKey(tc => tc.SecondCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderProductMap>()
                .HasOne(opm => opm.Order)
                .WithMany(o => o.OrderProductMaps)
                .HasForeignKey(opm => opm.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderProductMap>()
                .HasOne(opm => opm.Product)
                .WithMany()
                .HasForeignKey(opm => opm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure only one shop configuration exists
            modelBuilder.Entity<ShopConfiguration>()
                .HasIndex(s => s.Id)
                .IsUnique();

            modelBuilder.Entity<OrderHistory>()
                .HasOne(oh => oh.Order)
                .WithMany()
                .HasForeignKey(oh => oh.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderHistory>()
                .HasOne(oh => oh.User)
                .WithMany()
                .HasForeignKey(oh => oh.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Accounting indexes
            modelBuilder.Entity<AccountingEntry>()
                .HasIndex(ae => ae.EntryDate);

            modelBuilder.Entity<AccountingEntry>()
                .HasIndex(ae => ae.EntryType);

            modelBuilder.Entity<AccountingEntry>()
                .HasIndex(ae => ae.PaymentMethod);

            modelBuilder.Entity<AccountingEntry>()
                .HasIndex(ae => ae.CreatedBy);

            modelBuilder.Entity<DailySales>()
                .HasIndex(ds => ds.SaleDate)
                .IsUnique();
        }
    }
}

