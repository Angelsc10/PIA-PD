using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;

namespace PIA_PD.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Ventas()
        {
            var ventas = await _context.Ventas
                                .Include(v => v.Detalles)
                                .OrderByDescending(v => v.Fecha)
                                .ToListAsync();

            return View(ventas);
        }

        // --- NUEVA SECCIÓN: API DE ESTADÍSTICAS PARA LA GRÁFICA ---
        [HttpGet]
        public async Task<IActionResult> GetDatosVentas()
        {
            // Calculamos la fecha de hace 7 días
            var fechaInicio = DateTime.Now.Date.AddDays(-6);

            // Traemos las ventas de esa semana
            var ventas = await _context.Ventas
                .Where(v => v.Fecha >= fechaInicio)
                .ToListAsync();

            // Las agrupamos por día sumando el total de dinero
            var agrupado = ventas
                .GroupBy(v => v.Fecha.Date)
                .Select(g => new {
                    fecha = g.Key.ToString("dd/MM"),
                    total = g.Sum(v => v.Total)
                }).ToList();

            // Rellenamos los días que no tuvieron ventas con $0 para que la gráfica no se rompa
            var datosGrafica = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var diaTexto = DateTime.Now.Date.AddDays(-i).ToString("dd/MM");
                var ventaDia = agrupado.FirstOrDefault(v => v.fecha == diaTexto);

                datosGrafica.Add(new
                {
                    fecha = diaTexto,
                    total = ventaDia != null ? ventaDia.total : 0
                });
            }

            return Json(datosGrafica);
        }

        // --- SECCIÓN: GESTIÓN DE EMPLEADOS ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Empleados()
        {
            var empleados = await _userManager.GetUsersInRoleAsync("Empleado");
            return View(empleados);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AltaEmpleado(string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var nuevoEmpleado = new IdentityUser { UserName = username, Email = "" };
                var result = await _userManager.CreateAsync(nuevoEmpleado, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(nuevoEmpleado, "Empleado");
                }
                else
                {
                    TempData["Error"] = "La contraseña debe tener 8 caracteres, mayúscula, número y símbolo.";
                }
            }
            return RedirectToAction("Empleados");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BajaEmpleado(string id)
        {
            var empleado = await _userManager.FindByIdAsync(id);
            if (empleado != null)
            {
                await _userManager.DeleteAsync(empleado);
            }
            return RedirectToAction("Empleados");
        }
    }
}