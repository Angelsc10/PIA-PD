using Microsoft.AspNetCore.Mvc;
using PIA_PD.Models;
using PIA_PD.Extensions;

namespace PIA_PD.Controllers
{
    public class CarritoController : Controller
    {
        // Ver el carrito completo
        public IActionResult Index()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            return View(carrito);
        }

        // Agregar al carrito
        [HttpPost]
        public IActionResult Agregar(string id, string titulo, decimal precio, string portadaUrl)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var itemExistente = carrito.FirstOrDefault(i => i.LibroId == id);

            if (itemExistente != null)
            {
                itemExistente.Cantidad++;
            }
            else
            {
                carrito.Add(new CarritoItem
                {
                    LibroId = id,
                    Titulo = titulo,
                    Precio = precio,
                    PortadaUrl = portadaUrl,
                    Cantidad = 1
                });
            }

            HttpContext.Session.Set("MiCarrito", carrito);
            return RedirectToAction("Index", "Home");
        }

        // Eliminar un libro del carrito
        public IActionResult Eliminar(string id)
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(i => i.LibroId == id);

            if (item != null)
            {
                carrito.Remove(item);
            }

            HttpContext.Session.Set("MiCarrito", carrito);
            return RedirectToAction("Index");
        }

        // Procesar la compra y generar Ticket TXT
        [HttpPost]
        public IActionResult ProcesarPago()
        {
            var carrito = HttpContext.Session.Get<List<CarritoItem>>("MiCarrito");

            // Si el carrito está vacío por alguna razón, lo regresamos a la página
            if (carrito == null || !carrito.Any())
            {
                return RedirectToAction("Index");
            }

            // 1. Armar el diseño del ticket en texto plano
            var ticket = new System.Text.StringBuilder();
            ticket.AppendLine("========================================");
            ticket.AppendLine("           EL RINCÓN DEL LIBRO          ");
            ticket.AppendLine("            TICKET DE COMPRA            ");
            ticket.AppendLine("========================================");
            ticket.AppendLine($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            ticket.AppendLine($"Atendido por: Web Automatizada");
            ticket.AppendLine("----------------------------------------");
            ticket.AppendLine("CANT | PRODUCTO                 | SUBTOT");
            ticket.AppendLine("----------------------------------------");

            decimal totalVenta = 0;
            foreach (var item in carrito)
            {
                decimal subtotal = item.Precio * item.Cantidad;
                totalVenta += subtotal;

                // Formateamos para que se vea como una columna de ticket (cortamos títulos largos)
                string tituloCorto = item.Titulo.Length > 20 ? item.Titulo.Substring(0, 20) + "..." : item.Titulo.PadRight(23);
                ticket.AppendLine($" {item.Cantidad:00}  | {tituloCorto}| ${subtotal.ToString("0.00").PadLeft(6)}");
            }

            ticket.AppendLine("----------------------------------------");
            ticket.AppendLine($"                       TOTAL: ${totalVenta.ToString("0.00")}");
            ticket.AppendLine("========================================");
            ticket.AppendLine("   ¡Gracias por su preferencia e        ");
            ticket.AppendLine("   incentivar la lectura con nosotros!  ");
            ticket.AppendLine("========================================");

            // 2. Limpiar el carrito de la memoria para que vuelva a quedar en cero
            HttpContext.Session.Remove("MiCarrito");

            // 3. Convertir el texto a un archivo descargable
            var bytes = System.Text.Encoding.UTF8.GetBytes(ticket.ToString());
            string nombreArchivo = $"Ticket_Compra_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            return File(bytes, "text/plain", nombreArchivo);
        }
    }
}