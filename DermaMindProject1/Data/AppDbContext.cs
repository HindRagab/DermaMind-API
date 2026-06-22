using DermaApp.API.Controllers;
using DermaApp.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DermaApp.API.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<SkinTestQuestion> SkinTestQuestions { get; set; }
        public DbSet<SkinTypeProfile> SkinTypeProfiles { get; set; }
        public DbSet<SkinTestOption> SkinTestOptions { get; set; }
        public DbSet<SkinTestResult> SkinTestResults { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OtpEntry> OtpEntries { get; set; }
        public DbSet<DermaScanResult> DermaScanResults { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
    }
}