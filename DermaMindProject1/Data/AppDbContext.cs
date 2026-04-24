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
        public DbSet<SkinTestOption> SkinTestOptions { get; set; }
        public DbSet<SkinTestResult> SkinTestResults { get; set; }
    }
}