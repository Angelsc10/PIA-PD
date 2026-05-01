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

        public async Task<IActionResult> Index(string? q, string? categoria, int pagina = 1)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Empleado"))
            {
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.Categorias = new List<string> { "Ficción", "Romance", "Misterio", "Fantasía", "Ciencia Ficción", "Tecnología" };
            ViewBag.CategoriaActual = categoria;
            ViewBag.Busqueda = q;

            List<Libro> librosLocales;
            List<Libro> librosApi;

            if (!string.IsNullOrWhiteSpace(q))
            {
                librosLocales = await _context.LibrosInternos.AsNoTracking().Where(l => l.Titulo.Contains(q) || l.Autor.Contains(q)).ToListAsync();
                librosApi = await _apiService.BuscarLibrosAsync(q);
            }
            else if (!string.IsNullOrWhiteSpace(categoria))
            {
                librosLocales = await _context.LibrosInternos.AsNoTracking().Where(l => l.Categoria == categoria).ToListAsync();
                librosApi = await _apiService.ObtenerLibrosPorCategoriaAsync(categoria);
            }
            else
            {
                librosLocales = await _context.LibrosInternos.AsNoTracking().ToListAsync();
                librosApi = await _apiService.ObtenerLibrosDestacadosAsync();
            }

            // --- SINCRONIZACIÓN Y STOCK FIJO DE 10 ---
            foreach (var apiLibro in librosApi)
            {
                var coincidenciaEnDb = librosLocales.FirstOrDefault(l => l.Id == apiLibro.Id);
                if (coincidenciaEnDb != null)
                {
                    apiLibro.Stock = coincidenciaEnDb.Stock; // Si ya lo vendimos alguna vez, usa nuestro stock real
                }
                else
                {
                    apiLibro.Stock = 10; // Si es completamente nuevo, SIEMPRE son 10
                }
            }

            var todosLosLibros = librosLocales
                .Where(l => !l.Id.StartsWith("OL-"))
                .Concat(librosApi)
                .Where(l => l.Stock > 0)
                .ToList();

            int tamanoPagina = 12;
            int totalLibros = todosLosLibros.Count;
            var librosPaginados = todosLosLibros.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalLibros / (double)tamanoPagina);

            return View(librosPaginados);
        }

        public async Task<IActionResult> Detalles(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            Libro libro = null;
            var libroLocal = await _context.LibrosInternos.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);

            if (id.StartsWith("OL-"))
            {
                libro = await _apiService.ObtenerLibroPorIdAsync(id);
                if (libro != null)
                {
                    // Forzamos el stock a 10 o al stock de la base de datos
                    if (libroLocal != null)
                    {
                        libro.Stock = libroLocal.Stock;
                    }
                    else
                    {
                        libro.Stock = 10;
                    }
                }
            }
            else
            {
                libro = libroLocal;
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