using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PIA_PD.Models;

namespace PIA_PD.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Ponemos "false" por defecto para evitar el error del "RememberMe"
                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);

                if (result.Succeeded)
                {
                    // Buscamos por Usuario O por Correo para evitar el Error 500
                    var user = await _userManager.FindByNameAsync(model.UserName)
                               ?? await _userManager.FindByEmailAsync(model.UserName);

                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        // Si es Admin o Empleado, se va a su panel
                        if (roles.Contains("Admin") || roles.Contains("Empleado"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                    }
                    // Si es cliente, se va a la tienda
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión no válido.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // CORRECCIÓN: Quitamos el "model.Email" porque tu modelo no lo pide. Se queda en blanco.
                var user = new IdentityUser { UserName = model.UserName, Email = "" };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}