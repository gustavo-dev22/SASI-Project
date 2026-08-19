using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SASI.Infraestructura.Identity;
using SASI.Models;
using SASI.Servicios;

namespace SASI.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CuentaServicio _cuentaServicio;
        private readonly IAntiforgery Antiforgery;

        public CuentaController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            CuentaServicio cuentaServicio,
            IAntiforgery antiforgery)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _cuentaServicio = cuentaServicio;
            Antiforgery = antiforgery;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string userName, string password, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, mensaje = "Debe ingresar usuario y contraseña." });

            var resultado = await _cuentaServicio.LoginAsync(userName, password);

            if (!resultado.Success)
            {
                var respuesta = resultado.IntentosRestantes.HasValue
                    ? (object)new { success = false, tipo = "credencialesInvalidas", intentosRestantes = resultado.IntentosRestantes }
                    : new { success = false, tipo = "credencialesInvalidas" };
                return Json(respuesta);
            }

            if (resultado.OficinaId.HasValue && !string.IsNullOrEmpty(resultado.OficinaNombre))
            {
                HttpContext.Session.SetInt32("OficinaId", resultado.OficinaId.Value);
                HttpContext.Session.SetString("OficinaNombre", resultado.OficinaNombre);
            }

            if (resultado.RequiereCambioPassword)
            {
                HttpContext.Session.SetString("RequiereCambioPassword", "true");
                HttpContext.Session.SetString("CambioPasswordUserName", userName);
                return Json(new { success = false, tipo = "cambioPasswordObligatorio" });
            }

            if (resultado.PasswordVencida)
            {
                HttpContext.Session.SetString("PasswordVencida", "true");
                return Json(new { success = false, tipo = "cambioPasswordObligatorio" });
            }

            if (resultado.DiasRestantesPassword.HasValue)
            {
                HttpContext.Session.SetInt32("DiasRestantesPassword", resultado.DiasRestantesPassword.Value);
            }

            if (resultado.RolSeleccionado.HasValue)
            {
                HttpContext.Session.SetInt32("RolSeleccionado", resultado.RolSeleccionado.Value);
            }

            HttpContext.Session.Remove("MenuUsuario");
            HttpContext.Session.SetString("MenuUsuario", JsonConvert.SerializeObject(resultado.Menu ?? new List<MenuItemViewModel>()));

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("PasswordVencida");
            HttpContext.Session.Remove("RequiereCambioPassword");
            HttpContext.Session.Remove("CambioPasswordUserName");

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Cuenta");
        }

        [HttpPost]
        public IActionResult RenovarSesion()
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                return Ok();
            }

            return Unauthorized();
        }

        public IActionResult AccesoDenegado() => View("AccesoDenegado");

        [HttpPost]
        public async Task<IActionResult> SeleccionarRol(int rolId)
        {
            HttpContext.Session.SetInt32("RolSeleccionado", rolId);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var menu = await _cuentaServicio.SeleccionarRolAsync(user.Id, rolId);
            HttpContext.Session.SetString("MenuUsuario", JsonConvert.SerializeObject(menu));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> CambiarPasswordObligatorio()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.DebeCambiarPassword)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPasswordObligatorio(string userName, string nuevaPassword, string confirmarPassword)
        {
            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword != confirmarPassword)
            {
                TempData["ErrorCambioPassword"] = "Las contraseñas no coinciden o son inválidas.";
                TempData["MostrarModalPassword"] = true;
                return View();
            }

            var resultado = await _cuentaServicio.CambiarPasswordObligatorioAsync(userName, nuevaPassword);

            if (resultado.Exito)
            {
                HttpContext.Session.Remove("PasswordVencida");

                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Cuenta");
            }

            TempData["ErrorCambioPassword"] = resultado.Error ?? "Error al cambiar la contraseña.";
            TempData["MostrarModalPassword"] = true;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ObtenerTokenAntiForgery()
        {
            var tokens = Antiforgery.GetAndStoreTokens(HttpContext);
            return Json(new
            {
                token = tokens.RequestToken
            });
        }
    }
}
