using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SASI.Configuration;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using SASI.Infraestructura.Repositories;
using SASI.Models;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Web.Helpers;

namespace SASI.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;
        private readonly ISasiService _sasiService;
        private readonly int _sistemaId;
        private readonly int _diasVencimientoPassword;
        private readonly int _maximoIntentosFallidosLogin;
        private readonly IAntiforgery Antiforgery;
        private readonly SasiDbContext _sasiDbContext;

        public CuentaController(SignInManager<ApplicationUser> signInManager, 
                                UserManager<ApplicationUser> userManager, 
                                IUsuarioSistemaRepository usuarioSistemaRepository, IOptions<ConfiguracionSistemaSASI> config, 
                                ISasiService sasiService, IAntiforgery antiforgery, SasiDbContext sasiDbContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _sistemaId = config.Value.Id;
            _diasVencimientoPassword = config.Value.DiasVencimientoPassword;
            _maximoIntentosFallidosLogin = config.Value.IntentosFallidosPermitidosLogin;
            _sasiService = sasiService;
            Antiforgery = antiforgery;
            _sasiDbContext = sasiDbContext;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
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
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, mensaje = "Debe ingresar usuario y contraseña." });

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (!user.Activo)
                {
                    return Json(new { success = false, tipo = "inactivo" });
                }

                var tieneAccesoSASI = await _usuarioSistemaRepository.UsuarioTieneRolActivoEnSistemaAsync(user.Id, _sistemaId);
                if (!tieneAccesoSASI)
                {
                    return Json(new { success = false, tipo = "sinRol" });
                }

                if (user.IntentosFallidosConsecutivos >= _maximoIntentosFallidosLogin)
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                    return Json(new { success = false, tipo = "bloqueado" });
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    user.IntentosFallidosConsecutivos = 0;
                    await _userManager.UpdateAsync(user);

                    if (user.IdOficina.HasValue)
                    {
                        var oficina = await _sasiDbContext.Oficina
                            .FirstOrDefaultAsync(o => o.IdOficina == user.IdOficina.Value);

                        if (oficina != null)
                        {
                            HttpContext.Session.SetInt32("OficinaId", oficina.IdOficina);
                            HttpContext.Session.SetString("OficinaNombre", oficina.Nombre);
                        }
                    }

                    // Validar si debe cambiar contraseña (primer ingreso)
                    if (user.DebeCambiarPassword)
                    {
                        HttpContext.Session.SetString("RequiereCambioPassword", "true");
                        HttpContext.Session.SetString("CambioPasswordEmail", user.Email);
                        return Json(new { success = false, tipo = "cambioPasswordObligatorio" });
                    }

                    // Validar vencimiento de contraseña
                    if (user.FechaUltimoCambioPassword.HasValue)
                    {
                        var diasDesdeCambio = (DateTime.UtcNow - user.FechaUltimoCambioPassword.Value).TotalDays;
                        var diasRestantes = _diasVencimientoPassword - (int)diasDesdeCambio;
                        if (diasDesdeCambio >= _diasVencimientoPassword)
                        {
                            HttpContext.Session.SetString("PasswordVencida", "true");
                            return Json(new { success = false, tipo = "cambioPasswordObligatorio" });
                        }
                        else
                        {
                            HttpContext.Session.SetInt32("DiasRestantesPassword", diasRestantes);
                        }
                    }

                    var rolPredeterminado = await _usuarioSistemaRepository.ObtenerRolPredeterminado(user.Id, _sistemaId);
                    if (rolPredeterminado != null)
                        HttpContext.Session.SetInt32("RolSeleccionado", rolPredeterminado.Value);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    // Aquí llamas al endpoint de SASI
                    var infoUsuario = await _sasiService.ObtenerAccesosUsuario(user.UserName, password);

                    var sistemaActual = infoUsuario.Usuario.Sistemas
                                        .FirstOrDefault(s => s.Id == _sistemaId && s.Activo);

                    if (sistemaActual == null)
                    {
                        return Json(new { success = false, tipo = "sinAccesos" });
                    }

                    HttpContext.Session.Remove("MenuUsuario");

                    if (rolPredeterminado != null)
                    {
                        var rolActivo = sistemaActual.Roles.FirstOrDefault(r => r.IdRol == rolPredeterminado.Value && r.Activo);
                        if (rolActivo != null)
                        {
                            var objetosDelRolPrincipal = rolActivo.Objetos
                                .Where(o => o.Activo)
                                .ToList();

                            HttpContext.Session.SetString("MenuUsuario", JsonConvert.SerializeObject(objetosDelRolPrincipal));
                        }
                    }

                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                }
                else
                {
                    user.IntentosFallidosConsecutivos += 1;
                    await _userManager.UpdateAsync(user);

                    int intentosRestantes = _maximoIntentosFallidosLogin - user.IntentosFallidosConsecutivos;

                    if (user.IntentosFallidosConsecutivos >= _maximoIntentosFallidosLogin)
                    {
                        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                        return Json(new { success = false, tipo = "bloqueado" });
                    }
                    return Json(new { success = false, tipo = "credencialesInvalidas", intentosRestantes });
                }
            }

            return Json(new { success = false, tipo = "usuarioNoExiste" });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("PasswordVencida");
            HttpContext.Session.Remove("RequiereCambioPassword");
            HttpContext.Session.Remove("CambioPasswordEmail");

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Cuenta");
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult RenovarSesion()
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                return Ok();
            }

            return Unauthorized();
        }

        public IActionResult AccesoDenegado() => View("AccesoDenegado");

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeleccionarRol(int rolId)
        {
            HttpContext.Session.SetInt32("RolSeleccionado", rolId);

            var user = await _userManager.GetUserAsync(User);

            // Usa el nuevo método sin password
            var infoUsuario = await _sasiService.ObtenerAccesosUsuario(user.UserName);

            var sistemaActual = infoUsuario.Usuario.Sistemas
                                    .FirstOrDefault(s => s.Id == _sistemaId && s.Activo);

            if (sistemaActual != null)
            {
                var nuevoRol = sistemaActual.Roles.FirstOrDefault(r => r.IdRol == rolId && r.Activo);

                if (nuevoRol != null)
                {
                    var listaObjetos = nuevoRol.Objetos
                                        .Where(o => o.Activo)
                                        .GroupBy(o => o.IdObjeto)
                                        .Select(g => g.First())
                                        .ToList();

                    HttpContext.Session.SetString("MenuUsuario", JsonConvert.SerializeObject(listaObjetos));
                }
            }

            // Redirige a donde estabas, o al home
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
        public async Task<IActionResult> CambiarPasswordObligatorio(string email, string nuevaPassword, string confirmarPassword)
        {
            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword != confirmarPassword)
            {
                TempData["ErrorCambioPassword"] = "Las contraseñas no coinciden o son inválidas.";
                TempData["MostrarModalPassword"] = true;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);

            if (result.Succeeded)
            {
                TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                DateTime horaPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);

                user.FechaUltimoCambioPassword = horaPeru;
                user.DebeCambiarPassword = false;
                await _userManager.UpdateAsync(user);

                HttpContext.Session.Remove("PasswordVencida");

                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Cuenta");
            }

            TempData["ErrorCambioPassword"] = "Error al cambiar la contraseña.";
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