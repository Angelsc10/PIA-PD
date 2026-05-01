using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Extensions;
using PIA_PD.Models;
using PIA_PD.Services;

namespace PIA_PD.Controllers
{
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LibroApiService _apiService;

        public CarritoController(ApplicationDbContext context, LibroApiService apiService)
        {
            _context = context;
            _apiService = apiService;
        }

        public IActionResult Index()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            return View(carrito);
        }

        public IActionResult GetCarritoPartial()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            return PartialView("_CarritoPartial", carrito);
        }

        [HttpPost]
        public async Task<IActionResult> Agregar(string id, int cantidad)
        {
            int stockReal = await ObtenerStockDisponible(id);

            // Si el stock real desde el inicio es 0, rebota la petición
            if (stockReal <= 0) return Json(new { success = false, message = "Stock completamente agotado." });

            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(x => x.LibroId == id);
            int cantidadEnCarrito = item?.Cantidad ?? 0;

            // Validación estricta sincronizada con el Admin
            if ((cantidadEnCarrito + cantidad) > stockReal)
            {
                return Json(new { success = false, message = $"Solo quedan {stockReal} unidades en almacén." });
            }

            if (item == null)
            {
                var libroLocal = await _context.LibrosInternos.FindAsync(id);
                string t = libroLocal?.Titulo ?? "Libro";
                decimal p = libroLocal?.Precio ?? 0;
                string img = libroLocal?.PortadaUrl ?? "";

                if (id.StartsWith("OL-"))
                {
                    var librosApi = await _apiService.ObtenerLibrosDestacadosAsync();
                    var la = librosApi.FirstOrDefault(l => l.Id == id);
                    if (la != null) { t = la.Titulo; p = la.Precio; img = la.PortadaUrl; }
                }
                carrito.Add(new CarritoItem { LibroId = id, Titulo = t, Precio = p, PortadaUrl = img, Cantidad = cantidad });
            }
            else
            {
                item.Cantidad += cantidad;
            }

            HttpContext.Session.Set("MiCarrito", carrito);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad(string id, int cambio)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(x => x.LibroId == id);

            if (item != null)
            {
                int stockReal = await ObtenerStockDisponible(id);

                // Bloqueo estricto del botón '+' en el carrito lateral
                if (cambio > 0 && (item.Cantidad + cambio) > stockReal)
                {
                    return Json(new { success = false, message = "Límite de stock alcanzado." });
                }

                item.Cantidad += cambio;
                if (item.Cantidad <= 0) carrito.Remove(item);

                HttpContext.Session.Set("MiCarrito", carrito);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Eliminar(string id)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(x => x.LibroId == id);
            if (item != null)
            {
                carrito.Remove(item);
                HttpContext.Session.Set("MiCarrito", carrito);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            if (!carrito.Any()) return RedirectToAction("Index", "Home");
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> FinalizarCompra(string Email, string Telefono, string Calle, string CP, string Ciudad, string Estado, string Metodo)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            if (!carrito.Any()) return RedirectToAction("Index", "Home");

            var venta = new Venta { Usuario = User.Identity.Name, Email = Email, Fecha = DateTime.Now, Total = carrito.Sum(x => x.Precio * x.Cantidad), MetodoPago = Metodo ?? "Tarjeta" };

            foreach (var item in carrito)
            {
                if (item.LibroId.StartsWith("OL-"))
                {
                    var libroApiEnDb = await _context.LibrosInternos.FindAsync(item.LibroId);
                    if (libroApiEnDb == null)
                    {
                        int stockInicialApi = 10;
                        _context.LibrosInternos.Add(new Libro
                        {
                            Id = item.LibroId,
                            Titulo = item.Titulo,
                            Stock = stockInicialApi - item.Cantidad,
                            Precio = item.Precio,
                            PortadaUrl = item.PortadaUrl,
                            Autor = "API",
                            Categoria = "API"
                        });
                    }
                    else { libroApiEnDb.Stock -= item.Cantidad; }
                }
                else
                {
                    var l = await _context.LibrosInternos.FindAsync(item.LibroId);
                    if (l != null) l.Stock -= item.Cantidad;
                }
                venta.Detalles.Add(new DetalleVenta { LibroId = item.LibroId, Titulo = item.Titulo, Cantidad = item.Cantidad, PrecioUnitario = item.Precio });
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("MiCarrito");
            return RedirectToAction("Confirmar", new { id = venta.Id });
        }

        [Authorize]
        public async Task<IActionResult> Confirmar(int id)
        {
            var venta = await _context.Ventas.Include(v => v.Detalles).FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null) return RedirectToAction("Index", "Home");
            if (venta.Usuario != User.Identity.Name && venta.Usuario != "Cliente Anónimo") return RedirectToAction("Index", "Home");
            return View(venta);
        }

        private async Task<int> ObtenerStockDisponible(string id)
        {
            // 1. Siempre buscamos primero en la base de datos (La fuente de la verdad)
            var libroEnDb = await _context.LibrosInternos.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (libroEnDb != null) return libroEnDb.Stock;

            // 2. Si es de la API y NO está en la base de datos, el stock SIEMPRE es 10
            if (id.StartsWith("OL-"))
            {
                return 10;
            }

            return 0;
        }
    }
}