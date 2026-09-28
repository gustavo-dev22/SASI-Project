using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SASI.Aplicacion.Servicios;
using SASI.Dominio.Modelo;
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
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Sin intención SSO: se mantiene el acceso normal a la consola SASI.
                if (string.IsNullOrWhiteSpace(client_id))
                    return RedirectToAction("Index", "Home");

                // Sesión SASI ya activa + petición SSO: se emite el code sin pedir credenciales.
                var clienteActivo = await _ssoServicio.ObtenerClienteActivoAsync(client_id);
                if (clienteActivo == null || !_ssoServicio.RedirectUriValida(clienteActivo, returnUrl))
                {
                    _logger.LogWarning(
                        "SSO: solicitud no válida (cliente {ClientId}, returnUrl {ReturnUrl}).",
                        client_id, returnUrl);

                    ViewBag.ErrorSso = "Solicitud de acceso no válida.";
                    return View();
                }

                var usuario = await _userManager.GetUserAsync(User);
                if (usuario != null &&
                    await _usuarioSistemaServicio.UsuarioTieneRolActivoEnSistemaAsync(usuario.Id, clienteActivo.IdSistema))
                {
                    var codigo = await _ssoServicio.CrearAuthCodeAsync(
                        clienteActivo, usuario.Id, returnUrl!, code_challenge, code_challenge_method);

                    var sep = returnUrl!.Contains('?') ? '&' : '?';
                    var destino = $"{returnUrl}{sep}code={Uri.EscapeDataString(codigo)}";
                    if (!string.IsNullOrEmpty(state))
                        destino += $"&state={Uri.EscapeDataString(state)}";

                    return Redirect(destino);
                }

                // La sesión activa no sirve para el sistema solicitado: se cierra para permitir
                // el ingreso con otra cuenta, en lugar de enviar a la consola SASI.
                _logger.LogWarning(
                    "SSO: la sesión activa ({Usuario}) no tiene rol en el sistema {SistemaId} (cliente {ClientId}).",
                    usuario?.UserName ?? "(usuario no resuelto)", clienteActivo.IdSistema, clienteActivo.ClientId);

                await _signInManager.SignOutAsync();

                var sistema = await _sistemaServicio.ObtenerPorIdAsync(clienteActivo.IdSistema);
                ViewBag.NombreSistema = sistema?.Nombre;
                ViewBag.ClientId = client_id;
                ViewBag.State = state;
                ViewBag.CodeChallenge = code_challenge;
                ViewBag.CodeChallengeMethod = code_challenge_method;
                ViewBag.ErrorSso = "La sesión activa no tiene acceso a este sistema. Ingrese con otra cuenta.";
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            if (!string.IsNullOrWhiteSpace(client_id))
            {
                var cliente = await _ssoServicio.ObtenerClienteActivoAsync(client_id);
                if (cliente == null || !_ssoServicio.RedirectUriValida(cliente, returnUrl))
                {
                    ViewBag.ErrorSso = "Solicitud de acceso no válida.";
                    return View();
                }

                var sistema = await _sistemaServicio.ObtenerPorIdAsync(cliente.IdSistema);
                ViewBag.NombreSistema = sistema?.Nombre;
                ViewBag.ClientId = client_id;
                ViewBag.State = state;
                ViewBag.CodeChallenge = code_challenge;
                ViewBag.CodeChallengeMethod = code_challenge_method;
            }

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

            SistemaCliente? cliente = null;
            if (!string.IsNullOrWhiteSpace(client_id))
            {
                cliente = await _ssoServicio.ObtenerClienteActivoAsync(client_id);
                if (cliente == null || !_ssoServicio.RedirectUriValida(cliente, returnUrl))
                    return Json(new { success = false, tipo = "credencialesInvalidas", mensaje = "Solicitud SSO no válida." });
            }

            var resultado = cliente != null
                ? await _cuentaServicio.LoginAsync(userName, password, cliente.IdSistema)
                : await _cuentaServicio.LoginAsync(userName, password);

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

            // Flujo SSO: se emite el authorization code y se redirige al sistema externo.
            // No se configuran menú/rol de la consola SASI para no interferir con su sesión.
            if (cliente != null)
            {
                var code = await _ssoServicio.CrearAuthCodeAsync(
                    cliente, resultado.UserId, returnUrl!, code_challenge, code_challenge_method);

                var separador = returnUrl!.Contains('?') ? '&' : '?';
                var redirectSso = $"{returnUrl}{separador}code={Uri.EscapeDataString(code)}";
                if (!string.IsNullOrEmpty(state))
                    redirectSso += $"&state={Uri.EscapeDataString(state)}";

                return Json(new { success = true, redirectUrl = redirectSso });
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

        [HttpGet]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            // Tolerante a sesion/cookie expirada: cierra sesion siempre, sin exigir token antiforgery.
            try
            {
                HttpContext.Session.Remove("PasswordVencida");
                HttpContext.Session.Remove("RequiereCambioPassword");
                HttpContext.Session.Remove("CambioPasswordUserName");
                HttpContext.Session.Remove("RolSeleccionado");
                HttpContext.Session.Remove("MenuUsuario");
                _permisoUsuarioServicio.Limpiar();

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

            // Cierre de sesión único (SSO): si el destino pertenece a un cliente registrado,
            // se vuelve a él en lugar de la pantalla de login de SASI.
            if (await _ssoServicio.ReturnUrlPermitidoAsync(returnUrl))
                return Redirect(returnUrl!);

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
