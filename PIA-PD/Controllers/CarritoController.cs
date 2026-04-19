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
            // Verificamos si el libro es local y si tiene stock
            var libroDb = await _context.LibrosInternos.FindAsync(id);
            if (libroDb != null && libroDb.Stock <= 0)
            {
                TempData["Error"] = "Lo sentimos, este libro se ha agotado en bodega.";
                return RedirectToAction("Index", "Home");
            }

            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var itemExistente = carrito.FirstOrDefault(i => i.LibroId == id);

            if (itemExistente != null)
                itemExistente.Cantidad++;
            else
                carrito.Add(new CarritoItem { LibroId = id, Titulo = titulo, Precio = precio, PortadaUrl = portadaUrl, Cantidad = 1 });

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

            decimal totalVenta = carrito.Sum(i => i.Precio * i.Cantidad);

            var nuevaVenta = new Venta
            {
                Usuario = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Cliente Anónimo",
                Email = email,
                Fecha = DateTime.Now,
                Total = totalVenta
            };

            foreach (var item in carrito)
            {
                // ACTUALIZACIÓN DE STOCK EN MYSQL
                var libroDb = await _context.LibrosInternos.FindAsync(item.LibroId);
                if (libroDb != null)
                {
                    libroDb.Stock -= item.Cantidad;
                    if (libroDb.Stock < 0) libroDb.Stock = 0; // Seguridad
                }

                nuevaVenta.Detalles.Add(new DetalleVenta
                {
                    LibroId = item.LibroId,
                    Titulo = item.Titulo,
                    PrecioUnitario = item.Precio,
                    Cantidad = item.Cantidad
                });
            }

            _context.Ventas.Add(nuevaVenta);
            await _context.SaveChangesAsync();

            // Generar Ticket
            var ticket = new System.Text.StringBuilder();
            ticket.AppendLine("========================================");
            ticket.AppendLine("           EL RINCÓN DEL LIBRO          ");
            ticket.AppendLine($"         Folio de Venta: #{nuevaVenta.Id}       ");
            ticket.AppendLine("========================================");
            ticket.AppendLine($"Cliente: {nuevaVenta.Usuario}");
            ticket.AppendLine($"Total: ${totalVenta.ToString("0.0000")}");
            ticket.AppendLine("========================================");

            HttpContext.Session.Remove("MiCarrito");
            var bytes = System.Text.Encoding.UTF8.GetBytes(ticket.ToString());
            return File(bytes, "text/plain", $"Ticket_{nuevaVenta.Id}.txt");
        }
    }
}