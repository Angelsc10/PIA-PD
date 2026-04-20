using Microsoft.AspNetCore.Mvc;
using PIA_PD.Models;
using PIA_PD.Extensions;
using PIA_PD.Data;
using Microsoft.EntityFrameworkCore;

namespace PIA_PD.Controllers
{
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarritoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            return View(carrito);
        }

        [HttpPost]
        public async Task<IActionResult> Agregar(string id, string titulo, decimal precio, string portadaUrl)
        {
            var libroDb = await _context.LibrosInternos.FindAsync(id);
            if (libroDb != null && libroDb.Stock <= 0)
            {
                TempData["Error"] = "Lo sentimos, este libro se ha agotado en bodega.";
                return RedirectToAction("Index", "Home");
            }

            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var itemExistente = carrito.FirstOrDefault(i => i.LibroId == id);

            if (itemExistente != null) itemExistente.Cantidad++;
            else carrito.Add(new CarritoItem { LibroId = id, Titulo = titulo, Precio = precio, PortadaUrl = portadaUrl, Cantidad = 1 });

            HttpContext.Session.Set("MiCarrito", carrito);
            TempData["Exito"] = $"¡{titulo} agregado al carrito!";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Eliminar(string id)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(i => i.LibroId == id);
            if (item != null) carrito.Remove(item);
            HttpContext.Session.Set("MiCarrito", carrito);
            return RedirectToAction("Index");
        }

        // --- NUEVA SECCIÓN: LÓGICA DE CUPONES ---
        [HttpPost]
        public async Task<IActionResult> AplicarCupon(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return RedirectToAction("Index");

            var codigoUpper = codigo.ToUpper();

            // Truco de desarrollo: Si no existe el cupón "PROFE100", lo creamos mágicamente
            if (codigoUpper == "PROFE100" && !await _context.Cupones.AnyAsync(c => c.Codigo == "PROFE100"))
            {
                _context.Cupones.Add(new Cupon { Codigo = "PROFE100", PorcentajeDescuento = 15, Activo = true });
                await _context.SaveChangesAsync();
            }

            // Buscamos el cupón en MySQL
            var cupon = await _context.Cupones.FirstOrDefaultAsync(c => c.Codigo == codigoUpper && c.Activo);

            if (cupon != null)
            {
                HttpContext.Session.Set("MiCupon", cupon); // Guardamos el objeto cupón en la memoria
                TempData["Exito"] = $"¡Cupón aplicado! Tienes {cupon.PorcentajeDescuento}% de descuento.";
            }
            else
            {
                TempData["Error"] = "El cupón no existe o ha expirado.";
            }

            return RedirectToAction("Index");
        }

        public IActionResult RemoverCupon()
        {
            HttpContext.Session.Remove("MiCupon");
            TempData["Exito"] = "Cupón removido del carrito.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Confirmar()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito");
            if (carrito == null || !carrito.Any()) return RedirectToAction("Index");
            return View(carrito);
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarPago(string? email)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito");
            if (carrito == null || !carrito.Any()) return RedirectToAction("Index");

            decimal subtotal = carrito.Sum(i => i.Precio * i.Cantidad);
            decimal totalVenta = subtotal;
            decimal montoDescuento = 0;
            string? cuponCodigo = null;

            // Revisamos si hay un cupón activo para hacer la matemática
            var cuponAplicado = HttpContext.Session.Get<Cupon>("MiCupon");
            if (cuponAplicado != null)
            {
                montoDescuento = subtotal * (cuponAplicado.PorcentajeDescuento / 100m);
                totalVenta = subtotal - montoDescuento;
                cuponCodigo = cuponAplicado.Codigo;
            }

            var nuevaVenta = new Venta
            {
                Usuario = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Cliente Anónimo",
                Email = email,
                Fecha = DateTime.Now,
                Total = totalVenta,
                Descuento = montoDescuento,     // Registramos en BD
                CuponAplicado = cuponCodigo     // Registramos en BD
            };

            foreach (var item in carrito)
            {
                var libroDb = await _context.LibrosInternos.FindAsync(item.LibroId);
                if (libroDb != null)
                {
                    libroDb.Stock -= item.Cantidad;
                    if (libroDb.Stock < 0) libroDb.Stock = 0;
                }
                nuevaVenta.Detalles.Add(new DetalleVenta { LibroId = item.LibroId, Titulo = item.Titulo, PrecioUnitario = item.Precio, Cantidad = item.Cantidad });
            }

            _context.Ventas.Add(nuevaVenta);
            await _context.SaveChangesAsync();

            // Limpiamos todo al final
            HttpContext.Session.Remove("MiCarrito");
            HttpContext.Session.Remove("MiCupon");

            return RedirectToAction("VerTicketWeb", "Pedidos", new { id = nuevaVenta.Id });
        }
    }
}