using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SASI.Aplicacion.Servicios;
using SASI.Dominio.Modelo;
using SASI.Infraestructura.Identity;
using SASI.Models;
using SASI.Servicios;
using System.Security.Claims;

namespace SASI.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CuentaServicio _cuentaServicio;
        private readonly IPermisoUsuarioServicio _permisoUsuarioServicio;
        private readonly SsoServicio _ssoServicio;
        private readonly ISistemaServicio _sistemaServicio;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly IAntiforgery Antiforgery;
        private readonly ILogger<CuentaController> _logger;

        public CuentaController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            CuentaServicio cuentaServicio,
            IPermisoUsuarioServicio permisoUsuarioServicio,
            SsoServicio ssoServicio,
            ISistemaServicio sistemaServicio,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            IAntiforgery antiforgery,
            ILogger<CuentaController> logger)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _cuentaServicio = cuentaServicio;
            _permisoUsuarioServicio = permisoUsuarioServicio;
            _ssoServicio = ssoServicio;
            _sistemaServicio = sistemaServicio;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            Antiforgery = antiforgery;
        }

        [HttpGet]
        public async Task<IActionResult> Login(
            string? returnUrl = null,
            string? client_id = null,
            string? state = null,
            string? code_challenge = null,
            string? code_challenge_method = null)
        {
            // Sin intención SSO: comportamiento normal de la consola SASI.
            if (string.IsNullOrWhiteSpace(client_id))
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                    return RedirectToAction("Index", "Home");

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // Petición SSO: validar cliente y returnUrl.
            var cliente = await _ssoServicio.ObtenerClienteActivoAsync(client_id);
            if (cliente == null || !_ssoServicio.RedirectUriValida(cliente, returnUrl))
            {
                _logger.LogWarning(
                    "SSO: solicitud no válida (cliente {ClientId}, returnUrl {ReturnUrl}).",
                    client_id, returnUrl);

                ViewBag.ErrorSso = "Solicitud de acceso no válida.";
                return View();
            }

            // Identidad SSO (cookie propia, independiente de la consola SASI).
            var ssoAuth = await HttpContext.AuthenticateAsync(SasiAuthSchemes.Sso);
            if (ssoAuth.Succeeded)
            {
                var userIdClaim = ssoAuth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var usuario = Guid.TryParse(userIdClaim, out var guid)
                    ? await _userManager.FindByIdAsync(guid.ToString())
                    : null;

                if (usuario != null &&
                    await _usuarioSistemaServicio.UsuarioTieneRolActivoEnSistemaAsync(usuario.Id, cliente.IdSistema))
                {
                    var codigo = await _ssoServicio.CrearAuthCodeAsync(
                        cliente, usuario.Id, returnUrl!, code_challenge, code_challenge_method);

                    var sep = returnUrl!.Contains('?') ? '&' : '?';
                    var destino = $"{returnUrl}{sep}code={Uri.EscapeDataString(codigo)}";
                    if (!string.IsNullOrEmpty(state))
                        destino += $"&state={Uri.EscapeDataString(state)}";

                    return Redirect(destino);
                }

                _logger.LogWarning(
                    "SSO: la sesión SSO activa ({Usuario}) no tiene rol en el sistema {SistemaId} (cliente {ClientId}).",
                    usuario?.UserName ?? "(usuario no resuelto)", cliente.IdSistema, cliente.ClientId);

                ViewBag.ErrorSso = "La sesión activa no tiene acceso a este sistema. Ingrese con otra cuenta.";
            }

            var sistema = await _sistemaServicio.ObtenerPorIdAsync(cliente.IdSistema);
            ViewBag.NombreSistema = sistema?.Nombre;
            ViewBag.ClientId = client_id;
            ViewBag.State = state;
            ViewBag.CodeChallenge = code_challenge;
            ViewBag.CodeChallengeMethod = code_challenge_method;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string userName,
            string password,
            string? returnUrl = null,
            string? client_id = null,
            string? state = null,
            string? code_challenge = null,
            string? code_challenge_method = null)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, mensaje = "Debe ingresar usuario y contraseña." });

            // Flujo SSO: usa una sesión independiente de la consola SASI.
            if (!string.IsNullOrWhiteSpace(client_id))
            {
                return await ProcesarLoginSsoAsync(
                    userName, password, client_id, returnUrl, state, code_challenge, code_challenge_method);
            }

            // Flujo consola SASI.
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

            if (resultado.RolSeleccionado.HasValue)
            {
                await _permisoUsuarioServicio.EstablecerAsync(resultado.RolSeleccionado.Value);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }

        // Procesa el login del flujo SSO sin tocar la sesión de la consola SASI.
        private async Task<IActionResult> ProcesarLoginSsoAsync(
            string userName,
            string password,
            string clientId,
            string? returnUrl,
            string? state,
            string? codeChallenge,
            string? codeChallengeMethod)
        {
            var cliente = await _ssoServicio.ObtenerClienteActivoAsync(clientId);
            if (cliente == null || !_ssoServicio.RedirectUriValida(cliente, returnUrl))
                return Json(new { success = false, tipo = "credencialesInvalidas", mensaje = "Solicitud SSO no válida." });

            var resultado = await _cuentaServicio.LoginSsoAsync(userName, password, cliente.IdSistema);

            if (!resultado.Success)
            {
                var respuesta = resultado.IntentosRestantes.HasValue
                    ? (object)new { success = false, tipo = "credencialesInvalidas", intentosRestantes = resultado.IntentosRestantes }
                    : new { success = false, tipo = "credencialesInvalidas" };
                return Json(respuesta);
            }

            if (resultado.RequiereCambioPassword || resultado.PasswordVencida)
                return Json(new { success = false, tipo = "cambioPasswordObligatorio" });

            await FirmarSesionSsoAsync(resultado.UserId);

            var code = await _ssoServicio.CrearAuthCodeAsync(
                cliente, resultado.UserId, returnUrl!, codeChallenge, codeChallengeMethod);

            var separador = returnUrl!.Contains('?') ? '&' : '?';
            var redirectSso = $"{returnUrl}{separador}code={Uri.EscapeDataString(code)}";
            if (!string.IsNullOrEmpty(state))
                redirectSso += $"&state={Uri.EscapeDataString(state)}";

            return Json(new { success = true, redirectUrl = redirectSso });
        }

        // Establece la cookie de sesión SSO (esquema propio), sin afectar la consola.
        private async Task FirmarSesionSsoAsync(Guid usuarioId)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
            if (usuario == null)
                return;

            var principal = await _signInManager.CreateUserPrincipalAsync(usuario);
            await HttpContext.SignInAsync(SasiAuthSchemes.Sso, principal);
        }

        [HttpGet]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            // Elimina posibles cookies SSO residuales en otras rutas (p. ej. la raíz),
            // además de la que cierra el esquema SSO (path=/SASI), para garantizar el
            // cierre de sesión en todos los escenarios.
            Response.Cookies.Delete(SasiAuthSchemes.SsoCookieName, new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            // Cierre solicitado por una app consumidora: termina solo la sesión SSO,
            // dejando intacta la sesión de la consola SASI.
            if (await _ssoServicio.ReturnUrlPermitidoAsync(returnUrl))
            {
                try
                {
                    await HttpContext.SignOutAsync(SasiAuthSchemes.Sso);
                }
                catch (Exception)
                {
                    // sesión SSO no disponible: no bloquear el cierre
                }

                return Redirect(returnUrl!);
            }

            // Cierre de sesión de la consola SASI: cierra consola y sesión SSO.
            try
            {
                HttpContext.Session.Remove("PasswordVencida");
                HttpContext.Session.Remove("RequiereCambioPassword");
                HttpContext.Session.Remove("CambioPasswordUserName");
                HttpContext.Session.Remove("RolSeleccionado");
                HttpContext.Session.Remove("MenuUsuario");
                _permisoUsuarioServicio.Limpiar();

                await HttpContext.SignOutAsync(SasiAuthSchemes.Sso);
                await _signInManager.SignOutAsync();
            }
            catch (Exception)
            {
                // Si la sesion ya expiro, se ignora; el usuario queda sin autenticar de todos modos.
            }
            finally
            {
                try
                {
                    HttpContext.Session.Clear();
                }
                catch (Exception)
                {
                    // sesion no disponible: no bloquear el logout
                }
            }

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

            await _permisoUsuarioServicio.EstablecerAsync(rolId);

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
        public async Task<IActionResult> CambiarPasswordObligatorio(
            string userName,
            string nuevaPassword,
            string confirmarPassword,
            string? returnUrl = null,
            string? client_id = null,
            string? state = null,
            string? code_challenge = null,
            string? code_challenge_method = null)
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

                // Si el cambio de contraseña vino de un flujo SSO, se continúa con el
                // sistema de origen en lugar de enviar al usuario a la consola SASI.
                if (!string.IsNullOrWhiteSpace(client_id))
                {
                    var cliente = await _ssoServicio.ObtenerClienteActivoAsync(client_id);
                    if (cliente != null && _ssoServicio.RedirectUriValida(cliente, returnUrl))
                    {
                        var usuario = await _userManager.FindByNameAsync(userName);
                        if (usuario != null)
                        {
                            await FirmarSesionSsoAsync(usuario.Id);

                            var codigo = await _ssoServicio.CrearAuthCodeAsync(
                                cliente, usuario.Id, returnUrl!, code_challenge, code_challenge_method);

                            var sep = returnUrl!.Contains('?') ? '&' : '?';
                            var destino = $"{returnUrl}{sep}code={Uri.EscapeDataString(codigo)}";
                            if (!string.IsNullOrEmpty(state))
                                destino += $"&state={Uri.EscapeDataString(state)}";

                            return Redirect(destino);
                        }
                    }
                }

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
