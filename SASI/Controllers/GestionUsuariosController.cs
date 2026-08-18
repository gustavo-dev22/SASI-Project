using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Infraestructura.Identity;
using SASI.Models.Requests;
using SASI.Servicios;
using X.PagedList;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class GestionUsuariosController : Controller
    {
        private readonly GestionUsuariosServicio _servicio;

        public GestionUsuariosController(GestionUsuariosServicio servicio)
        {
            _servicio = servicio;
        }

        public IActionResult Index()
        {
            return View(new StaticPagedList<ApplicationUser>(new List<ApplicationUser>(), 1, 10, 0));
        }

        [HttpPost]
        public async Task<IActionResult> Buscar(string filtro, int page = 1)
        {
            var paged = await _servicio.BuscarAsync(filtro, page, 10);

            ViewBag.Filtro = filtro;

            return PartialView("_TablaUsuarios", paged);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] NuevoUsuarioRequest dto)
        {
            var resultado = await _servicio.CrearAsync(dto, User.Identity?.Name ?? string.Empty, HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Obtener(string id)
        {
            var usuario = await _servicio.ObtenerAsync(id);
            if (usuario == null) return NotFound();

            return Json(new
            {
                id = usuario.Id,
                nombreCompleto = usuario.NombreCompleto,
                email = usuario.Email,
                oficinaId = usuario.IdOficina,
                userName = usuario.UserName,
                bloqueado = usuario.LockoutEnabled && usuario.LockoutEnd != null,
                activo = usuario.Activo,
                intentosFallidosConsecutivos = usuario.IntentosFallidosConsecutivos
            });
        }

        [HttpPost]
        public async Task<IActionResult> Editar([FromForm] EditarUsuarioRequest dto)
        {
            var resultado = await _servicio.EditarAsync(dto, User.Identity?.Name ?? string.Empty, HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ObtenerSistemas()
        {
            var sistemas = await _servicio.ObtenerSistemasAsync();
            return Json(sistemas.Select(s => new { s.IdSistema, s.Nombre }));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ObtenerRolesPorSistema(int sistemaId)
        {
            var roles = await _servicio.ObtenerRolesPorSistemaAsync(sistemaId);
            return Json(roles.Select(r => new { r.IdRol, r.Nombre }));
        }

        [HttpPost]
        public async Task<IActionResult> AsignarSistemaARol([FromForm] UsuarioSistemaRequest dto)
        {
            var resultado = await _servicio.AsignarSistemaARolAsync(dto.UsuarioId.ToString(), dto.SistemaId, dto.RolId, dto.EsPrincipal);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarSistemasPorUsuario(Guid id)
        {
            var asignaciones = await _servicio.ObtenerSistemasPorUsuarioAsync(id);

            var resultado = asignaciones.Select(s => new {
                sistemaId = s.SistemaId,
                nombreSistema = s.NombreSistema,
                rolId = s.RolId,
                nombreRol = s.NombreRol,
                fechaAsignacion = s.FechaAsignacion.ToString("dd/MM/yyyy"),
                activo = s.Activo,
                esPrincipal = s.EsPrincipal
            });

            return Json(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> QuitarSistema([FromBody] QuitarSistemaRequest dto)
        {
            var resultado = await _servicio.QuitarSistemaAsync(dto.UsuarioId, dto.SistemaId);
            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoSistema([FromBody] CambiarEstadoSistemaRequest dto)
        {
            var resultado = await _servicio.CambiarEstadoSistemaAsync(dto.UsuarioId, dto.SistemaId, dto.RolId, dto.Activo);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarRolPrincipal(Guid usuarioId, int sistemaId, int rolPrincipalId)
        {
            await _servicio.ActualizarRolPrincipalAsync(usuarioId, sistemaId, rolPrincipalId);

            return Json(new { mensaje = "Rol principal actualizado correctamente" });
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarCargaMasiva([FromBody] List<UsuarioDto> usuarios)
        {
            if (usuarios == null || usuarios.Count == 0)
                return BadRequest(new { message = "No hay datos." });

            var resultado = await _servicio.ProcesarCargaMasivaAsync(usuarios, User.Identity?.Name ?? string.Empty, HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Ok(new { guardados = resultado.Guardados, errores = resultado.Errores, credenciales = resultado.Credenciales });
        }

        [HttpPost]
        public async Task<IActionResult> ValidarExistentes([FromBody] List<string> usuarios)
        {
            if (usuarios == null || usuarios.Count == 0)
                return BadRequest("No se enviaron usuarios.");

            var existentes = await _servicio.ValidarExistentesAsync(usuarios);
            return Ok(existentes);
        }
    }
}
