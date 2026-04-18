using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PIA_PD.Data
{
    // Aseguramos que herede de IdentityDbContext especificando IdentityUser
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public Microsoft.EntityFrameworkCore.DbSet<PIA_PD.Models.Libro> LibrosInternos { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // ¡ESTA LÍNEA ES VITAL! 
            // Carga todas las llaves primarias y relaciones de las tablas de Identity
            base.OnModelCreating(builder);

            // Aquí podrías personalizar nombres de tablas si quisieras en el futuro
        }
    }
}