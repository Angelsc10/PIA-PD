using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Models;

namespace PIA_PD.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public InventarioController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var libros = await _context.LibrosInternos.ToListAsync();
            return View(libros);
        }

        [HttpGet]
        public IActionResult Crear() => View();

        [HttpPost]
        public async Task<IActionResult> Crear(Libro libro, IFormFile? imagenPortada)
        {
            ModelState.Remove("imagenPortada");
            ModelState.Remove("PortadaUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(libro.Id)) libro.Id = Guid.NewGuid().ToString();

                    if (imagenPortada != null && imagenPortada.Length > 0)
                    {
                        string webRootPath = _hostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string carpetaDestino = Path.Combine(webRootPath, "img", "portadas");
                        if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                        string nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenPortada.FileName);
                        using (var fileStream = new FileStream(Path.Combine(carpetaDestino, nombreArchivo), FileMode.Create))
                        {
                            await imagenPortada.CopyToAsync(fileStream);
                        }
                        libro.PortadaUrl = "/img/portadas/" + nombreArchivo;
                    }
                    else if (string.IsNullOrEmpty(libro.PortadaUrl))
                    {
                        libro.PortadaUrl = "https://via.placeholder.com/300x400?text=Sin+Portada";
                    }

                    _context.LibrosInternos.Add(libro);
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Libro registrado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar: " + ex.Message;
                }
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
                TempData["Exito"] = "Libro eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}