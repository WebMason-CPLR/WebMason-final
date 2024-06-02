using Microsoft.EntityFrameworkCore;
using WebMason_final.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Identity;

namespace WebMason_final.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ServerOrder>()
                .HasOne(so => so.User)
                .WithMany(u => u.ServerOrders)
                .HasForeignKey(so => so.UserId);
        }
    }
}
