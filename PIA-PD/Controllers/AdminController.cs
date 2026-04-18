using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PIA_PD.Controllers
{
    // ¡LA MAGIA ESTÁ AQUÍ! Esta etiqueta bloquea a usuarios normales
    [Authorize(Roles = "Admin,Empleado")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}