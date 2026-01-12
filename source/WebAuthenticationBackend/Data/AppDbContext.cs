using WebAuthenticationBackend.Models.DatabaseObjects;
using Microsoft.EntityFrameworkCore;

namespace WebAuthenticationBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany() // A user can have many tokens (different browsers/devices)
                .HasForeignKey(rt => rt.UserId);
        }
    }
}
