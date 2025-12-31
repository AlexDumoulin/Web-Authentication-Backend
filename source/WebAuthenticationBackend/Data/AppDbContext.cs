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
        public DbSet<JwtUser> JwtUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<JwtUser>().HasKey(jwt => jwt.Id);
        }
    }
}
