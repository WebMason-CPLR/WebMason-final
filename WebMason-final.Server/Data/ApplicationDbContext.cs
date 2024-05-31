using Microsoft.EntityFrameworkCore;
using WebMason_final.Server.Models;

namespace WebMason_final.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Constructeur sans paramètres
        public ApplicationDbContext()
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<ServerOrder> ServerOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ServerOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.ServerOrders)
                      .HasForeignKey(e => e.UserId);
            });
        }
    }
}
