using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;

namespace PIA_PD.Controllers
{
    // Quitamos el [Authorize] de la clase completa para permitir que clientes anónimos vean su ticket recién comprado
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Solo usuarios registrados pueden ver su historial
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var misVentas = await _context.Ventas
                                .Include(v => v.Detalles)
                                .Where(v => v.Usuario == User.Identity.Name)
                                .OrderByDescending(v => v.Fecha)
                                .ToListAsync();

            return View(misVentas);
        }

        // NUEVA FUNCIÓN: Muestra el diseño del ticket web
        [HttpGet]
        public async Task<IActionResult> VerTicketWeb(int id)
        {
            var venta = await _context.Ventas
                                .Include(v => v.Detalles)
                                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null) return RedirectToAction("Index", "Home");

            // Filtro de Seguridad
            if (venta.Usuario != "Cliente Anónimo")
            {
                if (!User.Identity.IsAuthenticated || User.Identity.Name != venta.Usuario)
                {
                    TempData["Error"] = "No tienes permiso para ver este ticket privado.";
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(venta);
        }
    }
}