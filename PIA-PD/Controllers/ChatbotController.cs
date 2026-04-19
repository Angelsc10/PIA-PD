using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Services;

namespace PIA_PD.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly LibroApiService _libroService;
        private readonly ApplicationDbContext _context;

        // Le damos al bot acceso a la API y a MySQL
        public ChatbotController(LibroApiService libroService, ApplicationDbContext context)
        {
            _libroService = libroService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Preguntar(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return Json(new { respuesta = "Hola, soy el bot de la librería. ¿Qué libro estás buscando hoy?" });

            var busqueda = mensaje.ToLower();

            if (busqueda == "hola" || busqueda.Contains("buenos dias") || busqueda.Contains("buenas tardes") || busqueda.Contains("buenas noches"))
            {
                return Json(new { respuesta = "¡Hola! Bienvenido al Rincón del Libro. Puedes preguntarme si tenemos algún título en específico." });
            }

            // 1. Buscar en internet y en bodega local
            var librosApi = await _libroService.ObtenerLibrosDestacadosAsync();
            var librosLocales = await _context.LibrosInternos.ToListAsync();

            // 2. Unir ambos catálogos para que el bot revise todo
            var catalogoCompleto = librosLocales.Concat(librosApi).ToList();

            var libroEncontrado = catalogoCompleto.FirstOrDefault(l =>
                l.Titulo.ToLower().Contains(busqueda) ||
                busqueda.Contains(l.Titulo.ToLower()));

            // 3. Responder al usuario
            if (libroEncontrado != null)
            {
                return Json(new { respuesta = $"¡Excelentes noticias! Sí tenemos <strong>'{libroEncontrado.Titulo}'</strong> del autor {libroEncontrado.Autor}. El precio es de <span class='text-success fw-bold'>${libroEncontrado.Precio.ToString("0.00")} MXN</span>. ¡Búscalo en nuestro catálogo para agregarlo a tu carrito!" });
            }

            return Json(new { respuesta = "Lo siento, acabo de revisar la bodega física y el sistema digital, y no logro encontrar ese título por ahora. ¿Podrías intentar buscarlo con otra palabra?" });
        }
    }
}