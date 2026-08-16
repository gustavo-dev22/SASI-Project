using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public AuthController(UserManager<ApplicationUser> userManager, AutenticacionServicio autenticacionServicio)
        {
            _userManager = userManager;
            _autenticacionServicio = autenticacionServicio;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var resultado = await _autenticacionServicio.LoginAsync(request.UserName, request.Password);

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
