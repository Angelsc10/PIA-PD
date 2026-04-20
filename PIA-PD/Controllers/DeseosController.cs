using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;

namespace PIA_PD.Controllers
{
    [Authorize] // Solo usuarios con cuenta pueden tener lista de deseos
    public class DeseosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeseosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var deseos = await _context.Deseos
                .Where(d => d.Usuario == User.Identity.Name)
                .OrderByDescending(d => d.FechaAgregado)
                .ToListAsync();
            return View(deseos);
        }

        // --- EL CÓDIGO NUEVO (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> AlternarAjax(string id, string titulo, decimal precio, string portadaUrl)
        {
            var usuario = User.Identity.Name;

            // Verificamos si el libro ya está en los deseos
            var deseoExistente = await _context.Deseos
                .FirstOrDefaultAsync(d => d.LibroId == id && d.Usuario == usuario);

            if (deseoExistente != null)
            {
                // Si ya está, lo quitamos
                _context.Deseos.Remove(deseoExistente);
                await _context.SaveChangesAsync();
                return Json(new { success = true, agregado = false, message = "Eliminado de tu Lista de Deseos." });
            }
            else
            {
                // Si no está, lo agregamos
                var nuevoDeseo = new Deseo
                {
                    LibroId = id,
                    Usuario = usuario,
                    Titulo = titulo,
                    Precio = precio,
                    PortadaUrl = portadaUrl,
                    FechaAgregado = DateTime.Now
                };
                _context.Deseos.Add(nuevoDeseo);
                await _context.SaveChangesAsync();
                return Json(new { success = true, agregado = true, message = "¡Agregado a tu Lista de Deseos!" });
            }
        }
    }
}