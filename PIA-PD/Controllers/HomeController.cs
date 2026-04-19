using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;
using PIA_PD.Services;

namespace PIA_PD.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LibroApiService _apiService;

        // Inyectamos la base de datos y la API de libros
        public HomeController(ApplicationDbContext context, LibroApiService apiService)
        {
            _context = context;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Traemos los libros físicos que el Admin agregó a MySQL
                var librosLocales = await _context.LibrosInternos.ToListAsync();

                // 2. Traemos los libros mágicos de la API de OpenLibrary
                var librosApi = await _apiService.ObtenerLibrosDestacadosAsync();

                // 3. ¡Juntamos ambos mundos en una sola lista!
                var catalogoCompleto = librosLocales.Concat(librosApi).ToList();

                return View(catalogoCompleto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el catálogo: {ex.Message}");
                // Si no hay internet o falla la BD, mandamos una lista vacía para no crashear
                return View(new List<Libro>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}