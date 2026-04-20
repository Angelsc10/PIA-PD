using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;
using PIA_PD.Services;
using System.Diagnostics;

namespace PIA_PD.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LibroApiService _apiService;

        public HomeController(ApplicationDbContext context, LibroApiService apiService)
        {
            _context = context;
            _apiService = apiService;
        }

        // CAMBIO: Ahora acepta Búsqueda de texto (q) o Filtro de Categoría
        public async Task<IActionResult> Index(string? q, string? categoria)
        {
            // Mandamos los datos al menú lateral de HTML
            ViewBag.Categorias = new List<string> { "Ficción", "Romance", "Misterio", "Fantasía", "Ciencia Ficción", "Tecnología" };
            ViewBag.CategoriaActual = categoria;
            ViewBag.Busqueda = q;

            List<Libro> librosLocales;
            List<Libro> librosApi;

            // Prioridad 1: Si usaron la barra de búsqueda
            if (!string.IsNullOrWhiteSpace(q))
            {
                librosLocales = await _context.LibrosInternos
                    .Where(l => l.Titulo.Contains(q) || l.Autor.Contains(q)).ToListAsync();
                librosApi = await _apiService.BuscarLibrosAsync(q);
            }
            // Prioridad 2: Si le dieron clic a una categoría del menú lateral
            else if (!string.IsNullOrWhiteSpace(categoria))
            {
                librosLocales = await _context.LibrosInternos
                    .Where(l => l.Categoria == categoria).ToListAsync();
                librosApi = await _apiService.ObtenerLibrosPorCategoriaAsync(categoria);
            }
            // Prioridad 3: Pantalla principal por defecto
            else
            {
                librosLocales = await _context.LibrosInternos.ToListAsync();
                librosApi = await _apiService.ObtenerLibrosDestacadosAsync();
            }

            return View(librosLocales.Concat(librosApi).ToList());
        }

        public async Task<IActionResult> Detalles(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            Libro libro = null;
            if (id.StartsWith("OL-"))
            {
                libro = await _apiService.ObtenerLibroPorIdAsync(id);
            }
            else
            {
                libro = await _context.LibrosInternos.FirstOrDefaultAsync(l => l.Id == id);
            }

            if (libro == null) return NotFound();

            var resenas = await _context.Resenas
                .Where(r => r.LibroId == id)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            ViewBag.Resenas = resenas;
            ViewBag.Promedio = resenas.Any() ? Math.Round(resenas.Average(r => r.Calificacion), 1) : 0.0;

            return View(libro);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AgregarResena(string libroId, int calificacion, string comentario)
        {
            if (calificacion < 1 || calificacion > 5 || string.IsNullOrWhiteSpace(comentario))
            {
                TempData["Error"] = "Debes escribir un comentario y seleccionar las estrellas.";
                return RedirectToAction("Detalles", new { id = libroId });
            }

            var resena = new Resena { LibroId = libroId, Usuario = User.Identity.Name, Calificacion = calificacion, Comentario = comentario, Fecha = DateTime.Now };
            _context.Resenas.Add(resena);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "¡Gracias por tu reseña!";
            return RedirectToAction("Detalles", new { id = libroId });
        }

        public IActionResult Privacy() => View();
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}