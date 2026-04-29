using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;
using PIA_PD.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace PIA_PD.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LibroApiService _apiService;
        private readonly IWebHostEnvironment _hostEnvironment; // Para guardar imágenes localmente

        public InventarioController(ApplicationDbContext context, LibroApiService apiService, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _apiService = apiService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var librosLocales = await _context.LibrosInternos.ToListAsync();
            var librosApi = await _apiService.ObtenerLibrosDestacadosAsync();
            var todosLosLibros = librosLocales.Concat(librosApi).ToList();
            return View(todosLosLibros);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        // CAMBIO: Ahora recibe "imagenPortada" y maneja el guardado local y el placeholder
        [HttpPost]
        public async Task<IActionResult> Crear(Libro libro, IFormFile? imagenPortada)
        {
            if (ModelState.IsValid)
            {
                // Si el usuario subió una foto desde su PC...
                if (imagenPortada != null)
                {
                    string carpetaDestino = Path.Combine(_hostEnvironment.WebRootPath, "img", "portadas");
                    if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                    // Creamos un nombre único para que no choquen dos fotos iguales
                    string nombreArchivo = Guid.NewGuid().ToString() + "_" + imagenPortada.FileName;
                    string rutaFinal = Path.Combine(carpetaDestino, nombreArchivo);

                    using (var fileStream = new FileStream(rutaFinal, FileMode.Create))
                    {
                        await imagenPortada.CopyToAsync(fileStream);
                    }
                    libro.PortadaUrl = "/img/portadas/" + nombreArchivo;
                }
                // Si no subió foto y tampoco puso Link, ponemos el Placeholder "Sin Portada"
                else if (string.IsNullOrWhiteSpace(libro.PortadaUrl))
                {
                    libro.PortadaUrl = "https://via.placeholder.com/300x400?text=Sin+Portada";
                }

                libro.Id = Guid.NewGuid().ToString();
                _context.LibrosInternos.Add(libro);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Libro registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Por favor, verifica que los datos principales estén llenos.";
            return View(libro);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            if (id.StartsWith("OL-"))
            {
                TempData["Error"] = "Los libros de la API mundial son de solo lectura.";
                return RedirectToAction(nameof(Index));
            }

            var libro = await _context.LibrosInternos.FindAsync(id);
            if (libro == null) return NotFound();

            return View(libro);
        }

        // CAMBIO: También en el Editar aplicamos la misma lógica para fotos
        [HttpPost]
        public async Task<IActionResult> Editar(string id, Libro libro, IFormFile? imagenPortada)
        {
            if (id != libro.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (imagenPortada != null)
                {
                    string carpetaDestino = Path.Combine(_hostEnvironment.WebRootPath, "img", "portadas");
                    if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                    string nombreArchivo = Guid.NewGuid().ToString() + "_" + imagenPortada.FileName;
                    string rutaFinal = Path.Combine(carpetaDestino, nombreArchivo);

                    using (var fileStream = new FileStream(rutaFinal, FileMode.Create))
                    {
                        await imagenPortada.CopyToAsync(fileStream);
                    }
                    libro.PortadaUrl = "/img/portadas/" + nombreArchivo;
                }
                else if (string.IsNullOrWhiteSpace(libro.PortadaUrl))
                {
                    libro.PortadaUrl = "https://via.placeholder.com/300x400?text=Sin+Portada";
                }

                _context.Update(libro);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Libro actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Verifica los datos ingresados.";
            return View(libro);
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(string id)
        {
            if (id.StartsWith("OL-"))
            {
                TempData["Error"] = "No puedes borrar libros de la API mundial.";
                return RedirectToAction(nameof(Index));
            }

            var libro = await _context.LibrosInternos.FindAsync(id);
            if (libro != null)
            {
                _context.LibrosInternos.Remove(libro);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Libro eliminado del inventario.";
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> Crear(Libro libro)
        {
            // Este check valida las etiquetas [Required] y [Range] del Modelo
            if (ModelState.IsValid)
            {
                // Lógica para generar ID si es local
                if (string.IsNullOrEmpty(libro.Id))
                    libro.Id = Guid.NewGuid().ToString().Substring(0, 8);

                _context.Add(libro);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Libro registrado correctamente.";
                return RedirectToAction("Index");
            }

            // Si llegó aquí, es porque hubo un error (ej: pusieron un 0)
            // Se devuelve a la vista para mostrar los mensajes de error
            return View(libro);
        }
    }
}
            