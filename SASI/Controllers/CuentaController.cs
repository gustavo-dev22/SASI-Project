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
        private readonly int _sistemaId;
        private readonly int _diasVencimientoPassword;
        private readonly IAntiforgery Antiforgery;
        private readonly SasiDbContext _sasiDbContext;

        public CuentaController(SignInManager<ApplicationUser> signInManager, 
                                UserManager<ApplicationUser> userManager, 
                                IUsuarioSistemaRepository usuarioSistemaRepository, IOptions<ConfiguracionSistemaSASI> config, 
                                IAntiforgery antiforgery, SasiDbContext sasiDbContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _sistemaId = config.Value.Id;
            _diasVencimientoPassword = config.Value.DiasVencimientoPassword;
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

            if (user == null || !user.Activo)
            {
                return Json(new { success = false, tipo = "credencialesInvalidas" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                var intentosRestantes = Math.Max(0, _userManager.Options.Lockout.MaxFailedAccessAttempts - user.AccessFailedCount);
                return Json(new { success = false, tipo = "credencialesInvalidas", intentosRestantes });
            }

            var tieneAccesoSASI = await _usuarioSistemaRepository.UsuarioTieneRolActivoEnSistemaAsync(user.Id, _sistemaId);
            if (!tieneAccesoSASI)
            {
                await _signInManager.SignOutAsync();
                return Json(new { success = false, tipo = "credencialesInvalidas" });
            }

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

            var sistemasYRoles = await _usuarioSistemaRepository.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);

            if (!sistemasYRoles.Any(sr => sr.SistemaId == _sistemaId && sr.SistemaActivo && sr.UsuarioSistemaRolActivo))
            {
                await _signInManager.SignOutAsync();
                return Json(new { success = false, tipo = "credencialesInvalidas" });
            }

            HttpContext.Session.Remove("MenuUsuario");

            if (rolPredeterminado != null)
            {
                var rolActivo = sistemasYRoles
                    .FirstOrDefault(sr => sr.RolId == rolPredeterminado.Value && sr.SistemaId == _sistemaId && sr.UsuarioSistemaRolActivo);

                if (rolActivo != null)
                {
                    var objetosDelRolPrincipal = rolActivo.Objetos
                        .Where(o => o.Activo)
                        .Select(o => new MenuItemViewModel
                        {
                            Id = o.IdObjeto,
                            Nombre = o.Nombre,
                            Url = o.Url,
                            Icono = o.Icono ?? string.Empty,
                            Tipo = o.Tipo,
                            IdPadre = o.IdPadre,
                            Orden = o.Orden
                        })
                        .ToList();

                    HttpContext.Session.SetString("MenuUsuario", JsonConvert.SerializeObject(objetosDelRolPrincipal));
                }
            }

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
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
            if (user == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var sistemasYRoles = await _usuarioSistemaRepository.ObtenerSistemasYRolesDelUsuarioAsync(user.Id);

            if (sistemasYRoles.Any(sr => sr.SistemaId == _sistemaId && sr.SistemaActivo))
            {
                var nuevoRol = sistemasYRoles
                    .FirstOrDefault(sr => sr.RolId == rolId && sr.SistemaId == _sistemaId && sr.UsuarioSistemaRolActivo);

                if (nuevoRol != null)
                {
                    var listaObjetos = nuevoRol.Objetos
                                        .Where(o => o.Activo)
                                        .GroupBy(o => o.IdObjeto)
                                        .Select(g => g.First())
                                        .Select(o => new MenuItemViewModel
                                        {
                                            Id = o.IdObjeto,
                                            Nombre = o.Nombre,
                                            Url = o.Url,
                                            Icono = o.Icono ?? string.Empty,
                                            Tipo = o.Tipo,
                                            IdPadre = o.IdPadre,
                                            Orden = o.Orden
                                        })
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