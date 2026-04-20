using Microsoft.AspNetCore.Authorization; // <-- NUEVO: Librería de Seguridad
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

        // --- 1. LÓGICA AJAX PARA EL NUEVO CARRITO LATERAL ---
        public IActionResult GetCarritoPartial()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            return PartialView("_CarritoPartial", carrito);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarAjax(string id, string titulo, decimal precio, string portadaUrl)
        {
            var libroDb = await _context.LibrosInternos.FindAsync(id);
            if (libroDb != null && libroDb.Stock <= 0)
                return Json(new { success = false, message = "Libro agotado en bodega." });

            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(i => i.LibroId == id);

            if (item != null) item.Cantidad++;
            else carrito.Add(new CarritoItem { LibroId = id, Titulo = titulo, Precio = precio, PortadaUrl = portadaUrl, Cantidad = 1 });

            HttpContext.Session.Set("MiCarrito", carrito);
            return Json(new { success = true, count = carrito.Sum(i => i.Cantidad) });
        }

        [HttpPost]
        public IActionResult ActualizarCantidad(string id, int cambio)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(i => i.LibroId == id);
            if (item != null)
            {
                item.Cantidad += cambio;
                if (item.Cantidad <= 0) carrito.Remove(item);
            }
            HttpContext.Session.Set("MiCarrito", carrito);
            return RedirectToAction("GetCarritoPartial");
        }

        // --- 2. EL CARRITO TRADICIONAL ---
        public IActionResult Index()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            return View(carrito);
        }

        public IActionResult Eliminar(string id)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(i => i.LibroId == id);
            if (item != null) carrito.Remove(item);
            HttpContext.Session.Set("MiCarrito", carrito);
            return RedirectToAction("Index");
        }

        // --- 3. LÓGICA DE CUPONES DE DESCUENTO ---
        [HttpPost]
        public async Task<IActionResult> AplicarCupon(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return RedirectToAction("Index");

            var codigoUpper = codigo.ToUpper();
            if (codigoUpper == "PROFE100" && !await _context.Cupones.AnyAsync(c => c.Codigo == "PROFE100"))
            {
                _context.Cupones.Add(new Cupon { Codigo = "PROFE100", PorcentajeDescuento = 15, Activo = true });
                await _context.SaveChangesAsync();
            }

            var cupon = await _context.Cupones.FirstOrDefaultAsync(c => c.Codigo == codigoUpper && c.Activo);
            if (cupon != null)
            {
                HttpContext.Session.Set("MiCupon", cupon);
                TempData["Exito"] = $"¡Cupón aplicado! {cupon.PorcentajeDescuento}% de descuento.";
            }
            else
            {
                TempData["Error"] = "Cupón inválido.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult RemoverCupon()
        {
            HttpContext.Session.Remove("MiCupon");
            return RedirectToAction("Index");
        }

        // --- 4. NUEVO CHECKOUT DE 3 PASOS BLINDADO ---

        [HttpGet]
        [Authorize] // <-- BLINDAJE: Solo usuarios logueados pueden entrar aquí
        public IActionResult Checkout()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito");
            if (carrito == null || !carrito.Any()) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [Authorize] // <-- BLINDAJE: Solo usuarios logueados pueden procesar el pago
        public async Task<IActionResult> FinalizarCompra(string Email, string Telefono, string Calle, string CP, string Ciudad, string Estado, string Metodo)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito");
            if (carrito == null || !carrito.Any()) return RedirectToAction("Index", "Home");

            decimal subtotal = carrito.Sum(i => i.Precio * i.Cantidad);
            decimal totalVenta = subtotal;
            decimal montoDescuento = 0;
            string? cuponCodigo = null;

            var cuponAplicado = HttpContext.Session.Get<Cupon>("MiCupon");
            if (cuponAplicado != null)
            {
                montoDescuento = subtotal * (cuponAplicado.PorcentajeDescuento / 100m);
                totalVenta = subtotal - montoDescuento;
                cuponCodigo = cuponAplicado.Codigo;
            }

            var nuevaVenta = new Venta
            {
                Usuario = User.Identity.Name, // Ya sabemos que está logueado gracias al [Authorize]
                Email = Email,
                Fecha = DateTime.Now,
                Total = totalVenta,
                Descuento = montoDescuento,
                CuponAplicado = cuponCodigo
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

            // Limpiamos todo
            HttpContext.Session.Remove("MiCarrito");
            HttpContext.Session.Remove("MiCupon");

            // Mensaje de confirmación simulado
            TempData["Exito"] = $"¡Compra Exitosa! Se ha enviado la confirmación al correo {Email} y un SMS al {Telefono}. Pagaste mediante: {Metodo}";

            return RedirectToAction("VerTicketWeb", "Pedidos", new { id = nuevaVenta.Id });
        }
    }
}