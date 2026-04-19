using Microsoft.AspNetCore.Identity;

namespace PIA_PD.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "Admin", "Empleado", "Usuario" };
            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // Cambiamos el correo por un Username oficial
            var adminUsername = "AdminLibreria";
            var adminUser = await userManager.FindByNameAsync(adminUsername);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminUsername,
                    Email = "" // Ya no necesitamos correo
                };

                var result = await userManager.CreateAsync(newAdmin, "AdminP@ssw0rd1!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}