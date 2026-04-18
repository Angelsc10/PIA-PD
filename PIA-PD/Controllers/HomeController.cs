using Microsoft.AspNetCore.Mvc;
using PIA_PD.Models;
using PIA_PD.Services; // Importamos la carpeta de servicios
using System.Diagnostics;

namespace PIA_PD.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LibroApiService _libroApiService; // Agregamos el servicio

        // Inyectamos el servicio en el constructor
        public HomeController(ILogger<HomeController> logger, LibroApiService libroApiService)
        {
            _logger = logger;
            _libroApiService = libroApiService;
        }

        public async Task<IActionResult> Index()
        {
            // Traemos los libros de la API de Google
            var libros = await _libroApiService.ObtenerLibrosDestacadosAsync();

            // Mandamos los libros a la vista (HTML)
            return View(libros);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}