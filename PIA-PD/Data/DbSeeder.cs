using Microsoft.AspNetCore.Identity;

namespace PIA_PD.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Crear los 3 roles exactos que pidió el profesor
            string[] roles = { "Admin", "Empleado", "Usuario" };
            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // 2. Crear la cuenta del Administrador Supremo por defecto
            var adminEmail = "admin@libreria.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Le ponemos una contraseña que cumpla con tus validaciones estrictas
                var result = await userManager.CreateAsync(newAdmin, "AdminP@ssw0rd1!");
                if (result.Succeeded)
                {
                    // Le asignamos el rol de Jefe
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}