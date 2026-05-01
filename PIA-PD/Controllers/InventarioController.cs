using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;
using PIA_PD.Services;

namespace PIA_PD.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LibroApiService _apiService;

        public InventarioController(ApplicationDbContext context, LibroApiService apiService)
        {
            _context = context;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var librosLocales = await _context.LibrosInternos.ToListAsync();
            var librosApi = await _apiService.ObtenerLibrosDestacadosAsync();

            // --- SINCRONIZACIÓN MÁGICA PARA EL INVENTARIO ---
            foreach (var apiLibro in librosApi)
            {
                var coincidencia = librosLocales.FirstOrDefault(l => l.Id == apiLibro.Id);
                if (coincidencia != null)
                {
                    apiLibro.Stock = coincidencia.Stock; // Respeta tu base de datos
                }
                else
                {
                    apiLibro.Stock = 10; // Stock fijo para los nuevos de la API
                }
            }

            var todosLosLibros = librosLocales
                .Where(l => !l.Id.StartsWith("OL-")) // Evita duplicados
                .Concat(librosApi)
                .ToList();

            return View(todosLosLibros);
        }

        [HttpGet]
        public IActionResult Crear() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Libro libro)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(libro.PortadaUrl))
                {
                    libro.PortadaUrl = "https://via.placeholder.com/300x400?text=Sin+Portada";
                }

                if (string.IsNullOrEmpty(libro.Id))
                    libro.Id = Guid.NewGuid().ToString().Substring(0, 8);

                _context.LibrosInternos.Add(libro);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Libro registrado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            return View(libro);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            if (string.IsNullOrEmpty(id) || id.StartsWith("OL-")) return RedirectToAction(nameof(Index));
            var libro = await _context.LibrosInternos.FindAsync(id);
            return libro == null ? NotFound() : View(libro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string id, Libro libro)
        {
            if (id != libro.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(libro);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Cambios guardados.";
                return RedirectToAction(nameof(Index));
            }
            return View(libro);
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(string id)
        {
            var libro = await _context.LibrosInternos.FindAsync(id);
            if (libro != null)
            {
                _context.LibrosInternos.Remove(libro);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}