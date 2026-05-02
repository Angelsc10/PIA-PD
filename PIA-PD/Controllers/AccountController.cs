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
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = new[] { "Por favor, completa los campos requeridos." } });

            var user = await _userManager.FindByNameAsync(model.UserName)
                       ?? await _userManager.FindByEmailAsync(model.UserName);

            if (user == null)
                return Json(new { success = false, errors = new[] { "Error: Este nombre de usuario no existe en el sistema." } });

            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string url = (roles.Contains("Admin") || roles.Contains("Empleado"))
                             ? Url.Action("Index", "Admin")
                             : Url.Action("Index", "Home");

                return Json(new { success = true, redirectUrl = url });
            }

            return Json(new { success = false, errors = new[] { "Error: La contraseña es incorrecta." } });
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = new[] { "Por favor, revisa los datos ingresados." } });

            var user = new IdentityUser { UserName = model.UserName, Email = "" };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }

            // Agrupamos todos los errores para mostrarlos juntos
            var errorList = new List<string>();
            foreach (var error in result.Errors)
            {
                string msgError = error.Description;
                if (error.Code == "DuplicateUserName") msgError = $"El usuario '{model.UserName}' ya está registrado. Por favor, elige otro.";
                else if (error.Code == "PasswordTooShort") msgError = "Tu contraseña es muy corta. Debe tener al menos 6 caracteres.";
                else if (error.Code == "PasswordRequiresNonAlphanumeric") msgError = "La contraseña debe contener al menos un carácter especial (ej. @, #, !).";
                else if (error.Code == "PasswordRequiresDigit") msgError = "La contraseña debe contener al menos un número ('0'-'9').";
                else if (error.Code == "PasswordRequiresUpper") msgError = "La contraseña debe contener al menos una letra mayúscula ('A'-'Z').";

                errorList.Add(msgError);
            }

            return Json(new { success = false, errors = errorList });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}