using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;

namespace PIA_PD.Controllers
{
    [Authorize] // Blindaje: Solo usuarios con cuenta pueden tener lista de deseos
    public class DeseosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeseosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Mostrar la lista de deseos del usuario
        public async Task<IActionResult> Index()
        {
            var misDeseos = await _context.Deseos
                .Where(d => d.Usuario == User.Identity.Name)
                .OrderByDescending(d => d.FechaAgregado)
                .ToListAsync();

            return View(misDeseos);
        }

        // 2. Agregar o quitar un libro de la lista (Botón de Corazón)
        [HttpPost]
        public async Task<IActionResult> Alternar(string id, string titulo, decimal precio, string portadaUrl)
        {
            // Verificamos si ya le había dado "Me gusta"
            var existente = await _context.Deseos
                .FirstOrDefaultAsync(d => d.LibroId == id && d.Usuario == User.Identity.Name);

            if (existente != null)
            {
                _context.Deseos.Remove(existente);
                TempData["Exito"] = "Libro eliminado de tu lista de deseos.";
            }
            else
            {
                _context.Deseos.Add(new Deseo
                {
                    LibroId = id,
                    Titulo = titulo,
                    Precio = precio,
                    PortadaUrl = portadaUrl,
                    Usuario = User.Identity.Name
                });
                TempData["Exito"] = "¡Agregado a tu lista de deseos! ❤️";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        // 3. Eliminar desde la pantalla de la lista de deseos
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var deseo = await _context.Deseos.FindAsync(id);
            // Doble seguridad: Validar que exista y que sea del dueño
            if (deseo != null && deseo.Usuario == User.Identity.Name)
            {
                _context.Deseos.Remove(deseo);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Libro removido de favoritos.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}