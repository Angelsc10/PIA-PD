using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
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

            // Le pasamos a la vista si hay un cupón activo en la sesión
            ViewBag.CuponAplicado = HttpContext.Session.GetString("CuponAplicado");
            ViewBag.DescuentoPorcentaje = HttpContext.Session.GetInt32("DescuentoPorcentaje") ?? 0;

            return PartialView("_CarritoPartial", carrito);
        }

        // ==========================================
        // NUEVO: SISTEMA DE CUPONES POR AJAX
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> AplicarCupon(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return Json(new { success = false, message = "Escribe un código válido." });

            var cupon = await _context.Cupones.FirstOrDefaultAsync(c => c.Codigo == codigo && c.Activo);
            if (cupon == null)
                return Json(new { success = false, message = "El cupón no existe o ha expirado." });

            // Lo guardamos en la sesión para que "viaje" con el usuario
            HttpContext.Session.SetString("CuponAplicado", cupon.Codigo);
            HttpContext.Session.SetInt32("DescuentoPorcentaje", cupon.PorcentajeDescuento);

            return Json(new { success = true, message = $"¡Cupón del {cupon.PorcentajeDescuento}% aplicado correctamente!" });
        }

        [HttpPost]
        public IActionResult QuitarCupon()
        {
            HttpContext.Session.Remove("CuponAplicado");
            HttpContext.Session.Remove("DescuentoPorcentaje");
            return Json(new { success = true });
        }
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> Agregar(string id, int cantidad)
        {
            int stockReal = await ObtenerStockDisponible(id);
            if (stockReal <= 0) return Json(new { success = false, message = "Stock completamente agotado." });

            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(x => x.LibroId == id);
            int cantidadEnCarrito = item?.Cantidad ?? 0;

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
            else { item.Cantidad += cantidad; }

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

            // --- LECTURA DEL CUPÓN Y CÁLCULOS FINALES ---
            decimal subtotal = carrito.Sum(x => x.Precio * x.Cantidad);
            string cuponApp = HttpContext.Session.GetString("CuponAplicado");
            int descPorc = HttpContext.Session.GetInt32("DescuentoPorcentaje") ?? 0;

            decimal descuentoFinal = (subtotal * descPorc) / 100m;
            decimal totalFinal = subtotal - descuentoFinal;

            var venta = new Venta
            {
                Usuario = User.Identity.Name,
                Email = Email,
                Fecha = DateTime.Now,
                Total = totalFinal, // Se guarda el total ya con descuento
                MetodoPago = Metodo ?? "Tarjeta",
                CuponAplicado = cuponApp,     // Rellenamos tu tabla
                Descuento = descuentoFinal    // Rellenamos tu tabla[cite: 1]
            };

            foreach (var item in carrito)
            {
                if (item.LibroId.StartsWith("OL-"))
                {
                    var libroApiEnDb = await _context.LibrosInternos.FindAsync(item.LibroId);
                    if (libroApiEnDb == null)
                    {
                        _context.LibrosInternos.Add(new Libro
                        {
                            Id = item.LibroId,
                            Titulo = item.Titulo,
                            Stock = 10 - item.Cantidad,
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

            // Limpiamos todo al terminar la compra
            HttpContext.Session.Remove("MiCarrito");
            HttpContext.Session.Remove("CuponAplicado");
            HttpContext.Session.Remove("DescuentoPorcentaje");

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
            var libroEnDb = await _context.LibrosInternos.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (libroEnDb != null) return libroEnDb.Stock;
            if (id.StartsWith("OL-")) return 10;
            return 0;
        }
    }
}