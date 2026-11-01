using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASI.Aplicacion.Servicios;
using SASI.Authorization;
using SASI.Infraestructura.Identity;
using SASI.Models.Requests;
using SASI.Models.Response;
using SASI.Servicios;

namespace SASI.Controllers.API
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AutenticacionServicio _autenticacionServicio;
        private readonly SsoServicio _ssoServicio;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly ISistemaServicio _sistemaServicio;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            AutenticacionServicio autenticacionServicio,
            SsoServicio ssoServicio,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            ISistemaServicio sistemaServicio)
        {
            _userManager = userManager;
            _autenticacionServicio = autenticacionServicio;
            _ssoServicio = ssoServicio;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _sistemaServicio = sistemaServicio;
        }

        // Disponibilidad del servicio de autenticación. Permite a los sistemas consumidores
        // avisar al usuario si SASI no está accesible antes de redirigir al login SSO.
        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping() => Ok(new { ok = true });

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            int? sistemaId = request.SistemaId;

            // Si viene client_id, se resuelve el sistema del cliente (tiene prioridad).
            if (!string.IsNullOrWhiteSpace(request.ClientId))
            {
                var cliente = await _ssoServicio.ObtenerClienteActivoAsync(request.ClientId);
                if (cliente == null)
                {
                    return Ok(new
                    {
                        success = false,
                        codigo = "CLIENTE_INVALIDO",
                        message = "Cliente no válido o inactivo.",
                        bloqueado = false
                    });
                }

                sistemaId = cliente.IdSistema;
            }

            var resultado = await _autenticacionServicio.LoginAsync(request.UserName, request.Password, sistemaId);

            if (resultado == null)
            {
                return Ok(new
                {
                    success = false,
                    codigo = "CREDENCIALES_INCORRECTAS",
                    message = "Usuario o contraseña incorrectos",
                    bloqueado = false
                });
            }

            return Ok(resultado);
        }

        // Canje del authorization code emitido por el flujo SSO (OAuth2 authorization_code + PKCE).
        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<IActionResult> Token([FromBody] SsoTokenRequest request)
        {
            if (request == null || !string.Equals(request.GrantType, "authorization_code", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, error = "unsupported_grant_type" });

            var canje = await _ssoServicio.CanjearAsync(
                request.ClientId,
                request.ClientSecret,
                request.Code,
                request.CodeVerifier,
                request.RedirectUri);

            if (!canje.Exito)
                return BadRequest(new { success = false, error = "invalid_grant", error_description = canje.Error });

            // Endurecimiento: el usuario debe seguir teniendo acceso al sistema del cliente
            // (cubre una revocación de rol entre la emisión del code y su canje).
            if (!await _usuarioSistemaServicio.UsuarioTieneRolActivoEnSistemaAsync(canje.UsuarioId, canje.SistemaId))
            {
                var sistema = await _sistemaServicio.ObtenerPorIdAsync(canje.SistemaId);
                var mensaje = sistema == null
                    ? "Su usuario no tiene acceso a este sistema."
                    : sistema.Nombre.StartsWith("Sistema", StringComparison.OrdinalIgnoreCase)
                        ? $"Su usuario no tiene acceso a {sistema.Nombre}."
                        : $"Su usuario no tiene acceso al sistema {sistema.Nombre}.";

                return BadRequest(new
                {
                    success = false,
                    codigo = "SIN_ACCESO",
                    error = "access_denied",
                    error_description = mensaje,
                    sistemaId = canje.SistemaId,
                    sistemaNombre = sistema?.Nombre
                });
            }

            var sesion = await _autenticacionServicio.EmitirSesionAsync(canje.UsuarioId);
            if (sesion == null)
                return Unauthorized(new { success = false, error = "invalid_grant", error_description = "Usuario no habilitado." });

            return Ok(sesion);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var resultado = await _autenticacionServicio.RefreshAsync(request.RefreshToken);

            if (resultado == null)
            {
                return Unauthorized(new { success = false, message = "Refresh token inválido o expirado." });
            }

            return Ok(resultado);
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
        {
            var exito = await _autenticacionServicio.RevokeAsync(request.RefreshToken);

            if (!exito)
            {
                return NotFound(new { success = false, message = "Refresh token no encontrado." });
            }

            return Ok(new { success = true, message = "Sesión revocada correctamente." });
        }

        [HttpGet("accesos-usuario/{userName}")]
        public async Task<IActionResult> ObtenerAccesosPorUsuario(string userName)
        {
            var resultado = await _autenticacionServicio.ObtenerAccesosAsync(userName);

            if (resultado == null)
                return NotFound("Usuario no encontrado");

            return Ok(resultado);
        }

        [HttpGet("usuarios/{id}/basico")]
        public async Task<IActionResult> ObtenerUsuarioBasico(Guid id)
        {
            var user = await _userManager.Users
                .Where(u => u.Id == id)
                .Select(u => new UsuarioBasicoResponse
                {
                    IdUsuario = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email ?? ""
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost("usuarios/basicos")]
        public async Task<IActionResult> ObtenerUsuariosBasicos([FromBody] List<Guid> ids)
        {
            var usuarios = await _userManager.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new UsuarioBasicoResponse
                {
                    IdUsuario = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email ?? ""
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpPost("sga/crear-alumno")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RolesSasi.Administracion)]
        public async Task<IActionResult> CrearAlumnoDesdeSga([FromBody] NuevoUsuarioApiRequest dto)
        {
            var resultado = await _autenticacionServicio.CrearAlumnoAsync(dto);

            if (!resultado.Exito && resultado.Codigo == AutenticacionServicio.CodigoEmailExistente)
                return Conflict(new { message = resultado.Error });

            if (!resultado.Exito)
                return BadRequest(new { success = false, message = resultado.Error });

            return Ok(resultado.Resultado);
        }
    }
}
