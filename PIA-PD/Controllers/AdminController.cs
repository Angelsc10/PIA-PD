using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIA_PD.Data;
using PIA_PD.Services; // IMPORTANTE: Para leer la API
using ClosedXML.Excel;

namespace PIA_PD.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly LibroApiService _apiService; // NUEVO: Radar de la API

        // Inyectamos el servicio de la API en el constructor
        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, LibroApiService apiService)
        {
            _context = context;
            _userManager = userManager;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Buscamos stock crítico en los libros Físicos (Locales)
            var alertasLocales = await _context.LibrosInternos
                .Where(l => l.Stock < 5)
                .ToListAsync();

            // 2. Buscamos stock crítico en los libros de la API
            var librosApi = await _apiService.ObtenerLibrosDestacadosAsync();
            var alertasApi = librosApi.Where(l => l.Stock < 5).ToList();

            // 3. Unimos ambas listas para mostrarlas en el panel
            var alertasStock = alertasLocales.Concat(alertasApi)
                .OrderBy(l => l.Stock)
                .ToList();

            var topVentas = await _context.DetallesVenta
                .GroupBy(d => d.Titulo)
                .Select(g => new TopVentaDto
                {
                    Titulo = g.Key,
                    CantidadVendida = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(x => x.CantidadVendida)
                .Take(3)
                .ToListAsync();

            ViewBag.AlertasStock = alertasStock;
            ViewBag.TopVentas = topVentas;

            return View();
        }

        public async Task<IActionResult> Ventas()
        {
            var ventas = await _context.Ventas
                                .Include(v => v.Detalles)
                                .OrderByDescending(v => v.Fecha)
                                .ToListAsync();
            return View(ventas);
        }

        [HttpGet]
        public async Task<IActionResult> GetDatosVentas()
        {
            var fechaInicio = DateTime.Now.Date.AddDays(-6);
            var ventas = await _context.Ventas.Where(v => v.Fecha >= fechaInicio).ToListAsync();

            var agrupado = ventas.GroupBy(v => v.Fecha.Date)
                .Select(g => new { fecha = g.Key.ToString("dd/MM"), total = g.Sum(v => v.Total) }).ToList();

            var datosGrafica = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var diaTexto = DateTime.Now.Date.AddDays(-i).ToString("dd/MM");
                var ventaDia = agrupado.FirstOrDefault(v => v.fecha == diaTexto);
                datosGrafica.Add(new { fecha = diaTexto, total = ventaDia != null ? ventaDia.total : 0 });
            }
            return Json(datosGrafica);
        }

        // ================= GESTIÓN DE EMPLEADOS =================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Empleados()
        {
            var empleados = await _userManager.GetUsersInRoleAsync("Empleado");
            return View(empleados);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AltaEmpleado(string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var nuevoEmpleado = new IdentityUser { UserName = username, Email = "" };
                var result = await _userManager.CreateAsync(nuevoEmpleado, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(nuevoEmpleado, "Empleado");
                    TempData["Exito"] = "Empleado contratado exitosamente.";
                }
                else TempData["Error"] = "La contraseña debe tener 8 caracteres, mayúscula, número y símbolo.";
            }
            return RedirectToAction("Empleados");
        }

        // NUEVO: Pantalla para Editar Empleado
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarEmpleado(string id)
        {
            var empleado = await _userManager.FindByIdAsync(id);
            if (empleado == null) return NotFound();
            return View(empleado);
        }

        // NUEVO: Procesar la edición del Empleado
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarEmpleado(string id, string newUsername, string newPassword)
        {
            var empleado = await _userManager.FindByIdAsync(id);
            if (empleado != null)
            {
                if (!string.IsNullOrWhiteSpace(newUsername))
                    empleado.UserName = newUsername;

                var updateResult = await _userManager.UpdateAsync(empleado);

                // Si se escribió una nueva contraseña, la reseteamos
                if (updateResult.Succeeded && !string.IsNullOrWhiteSpace(newPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(empleado);
                    await _userManager.ResetPasswordAsync(empleado, token, newPassword);
                }

                TempData["Exito"] = "Datos del empleado actualizados.";
            }
            return RedirectToAction("Empleados");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BajaEmpleado(string id)
        {
            var empleado = await _userManager.FindByIdAsync(id);
            if (empleado != null)
            {
                await _userManager.DeleteAsync(empleado);
                TempData["Exito"] = "Empleado dado de baja.";
            }
            return RedirectToAction("Empleados");
        }

        // ================= EXPORTACIÓN =================
        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            var ventas = await _context.Ventas.Include(v => v.Detalles).OrderByDescending(v => v.Fecha).ToListAsync();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Ventas");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "Folio de Venta";
                worksheet.Cell(currentRow, 2).Value = "Fecha y Hora";
                worksheet.Cell(currentRow, 3).Value = "Cliente";
                worksheet.Cell(currentRow, 4).Value = "Total de Artículos";
                worksheet.Cell(currentRow, 5).Value = "Total Pagado (MXN)";

                var rangoCabecera = worksheet.Range(currentRow, 1, currentRow, 5);
                rangoCabecera.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                rangoCabecera.Style.Font.FontColor = XLColor.White;
                rangoCabecera.Style.Font.Bold = true;

                foreach (var venta in ventas)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = $"#{venta.Id}";
                    worksheet.Cell(currentRow, 2).Value = venta.Fecha.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(currentRow, 3).Value = venta.Usuario;
                    worksheet.Cell(currentRow, 4).Value = venta.Detalles.Sum(d => d.Cantidad);
                    worksheet.Cell(currentRow, 5).Value = venta.Total;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$ #,##0.00";
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Ventas_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }
    }

    public class TopVentaDto
    {
        public string Titulo { get; set; } = "";
        public int CantidadVendida { get; set; }
    }
}