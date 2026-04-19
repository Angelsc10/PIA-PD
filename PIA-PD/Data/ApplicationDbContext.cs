using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Models;

namespace PIA_PD.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<Libro> LibrosInternos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Libro>().HasKey(l => l.Id);
        }
    }
}