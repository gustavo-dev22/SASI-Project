using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using SASI.Infraestructura.Repositories;
using SASI.Models.Requests;

namespace SASI.Controllers
{
    public class UsuarioDto
    {
        public string NombreCompleto { get; set; }
        public string Usuario { get; set; } // esto será el UserName
        public string Email { get; set; }
        public string NombreOficina { get; set; }
    }

    [Authorize]
    public class GestionUsuariosController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISistemaRepository _sistemaRepository;
        private readonly IRolRepository _rolRepository;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;
        private readonly IOficinaRepository _oficinaRepository;

        public GestionUsuariosController(UserManager<ApplicationUser> userManager, 
                                         ISistemaRepository sistemaRepository, 
                                         IRolRepository rolRepository, 
                                         IUsuarioSistemaRepository usuarioSistemaRepository, 
                                         IOficinaRepository oficinaRepository)
        {
            _userManager = userManager;
            _sistemaRepository = sistemaRepository;
            _rolRepository = rolRepository;
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _oficinaRepository = oficinaRepository;
        }

        public IActionResult Index()
        {
            return View(new List<ApplicationUser>());
        }

        [HttpPost]
        public async Task<IActionResult> Buscar(string filtro)
        {
            var usuarios = await _userManager.Users
                .Where(u => u.NombreCompleto.Contains(filtro))
                .ToListAsync();

            return PartialView("_TablaUsuarios", usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] NuevoUsuarioRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.UserName) ||
                string.IsNullOrWhiteSpace(dto.NombreCompleto))
            {
                return Json(new { success = false, message = "Todos los campos son obligatorios." });
            }

            var existe = await _userManager.FindByEmailAsync(dto.Email);
            if (existe != null)
            {
                return Json(new { success = false, message = "El correo ya está registrado." });
            }

            TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            DateTime horaPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);

            var usuario = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                IdOficina = dto.OficinaId,
                AuditUsuarioCreacion = User.Identity?.Name ?? string.Empty,
                AuditFechaCreacion = DateTime.Now,
                IpCreacion = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty,
                Activo = true,
                IntentosFallidosConsecutivos = 0,
                DebeCambiarPassword = true,
                FechaUltimoCambioPassword = horaPeru
            };

            var resultado = await _userManager.CreateAsync(usuario, "Admin123.");

            if (resultado.Succeeded)
            {
                return Json(new { success = true, message = "Usuario registrado correctamente." });
            }

            var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
            return Json(new { success = false, message = "Error: " + errores });
        }

        [HttpGet]
        public async Task<IActionResult> Obtener(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
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
            var usuario = await _userManager.FindByIdAsync(dto.Id);
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado." });

            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.Email = dto.Email;
            usuario.IdOficina = dto.OficinaId;
            usuario.UserName = dto.UserName;
            usuario.NormalizedEmail = dto.Email.ToUpper();
            usuario.NormalizedUserName = dto.Email.ToUpper();
            usuario.Activo = dto.Activo;

            usuario.AuditUsuarioModificacion = User.Identity?.Name;
            usuario.AuditFechaModificacion = DateTime.Now;
            usuario.IpModificacion = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (dto.Bloqueado)
            {
                usuario.IntentosFallidosConsecutivos = 3;
                usuario.LockoutEnabled = true;
                usuario.LockoutEnd = DateTimeOffset.MaxValue; // bloqueo manual
            }
            else
            {
                usuario.IntentosFallidosConsecutivos = 0;
                usuario.LockoutEnabled = true; // mantener el sistema activo
                usuario.LockoutEnd = null;     // desbloqueo
                await _userManager.ResetAccessFailedCountAsync(usuario); // muy importante
            }

            var resultado = await _userManager.UpdateAsync(usuario);
            if (resultado.Succeeded)
                return Json(new { success = true, message = "Usuario actualizado correctamente." });

            var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
            return Json(new { success = false, message = errores });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerSistemas()
        {
            var sistemas = await _sistemaRepository.ListarAsync();
            return Json(sistemas.Select(s => new { s.IdSistema, s.Nombre }));
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRolesPorSistema(int sistemaId)
        {
            var roles = await _rolRepository.ObtenerPorSistemaId(sistemaId);
            return Json(roles.Select(r => new { r.IdRol, r.Nombre }));
        }

        [HttpPost]
        public async Task<IActionResult> AsignarSistemaARol([FromForm] UsuarioSistemaRequest dto)
        {
            var resultado = await _usuarioSistemaRepository.AsignarUsuarioASistemaAsync(dto.UsuarioId.ToString(), dto.SistemaId, dto.RolId, dto.EsPrincipal);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpGet]
        public async Task<IActionResult> ListarSistemasPorUsuario(Guid id)
        {
            var asignaciones = await _usuarioSistemaRepository.ObtenerSistemasPorUsuarioAsync(id);

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
            var exito = await _usuarioSistemaRepository.QuitarUsuarioDeSistemaAsync(dto.UsuarioId, dto.SistemaId);
            if (exito)
                return Json(new { success = true, message = "Sistema eliminado correctamente." });

            return Json(new { success = false, message = "No se pudo eliminar el sistema." });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoSistema([FromBody] CambiarEstadoSistemaRequest dto)
        {
            var resultado = await _usuarioSistemaRepository.ActualizarEstadoSistemaAsync(dto.UsuarioId, dto.SistemaId, dto.RolId, dto.Activo);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarRolPrincipal(Guid usuarioId, int sistemaId, int rolPrincipalId)
        {
            await _usuarioSistemaRepository.ActualizarRolPrincipalAsync(usuarioId, sistemaId, rolPrincipalId);

            return Json(new { mensaje = "Rol principal actualizado correctamente" });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProcesarCargaMasiva([FromBody] List<UsuarioDto> usuarios)
        {
            if (usuarios == null || usuarios.Count == 0)
                return BadRequest(new { message = "No hay datos." });

            int guardados = 0;
            var errores = new List<object>();

            // Opcional: precargar usuarios existentes para validar duplicados rápido
            var existentes = _userManager.Users
                .Select(u => new { u.UserName, u.Email })
                .ToList();

            foreach (var u in usuarios)
            {
                try
                {
                    // Validaciones básicas
                    if (string.IsNullOrWhiteSpace(u.Usuario))
                    {
                        errores.Add(new { Usuario = u.Usuario, Motivo = "El nombre de usuario está vacío." });
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(u.Email) || !IsValidEmail(u.Email))
                    {
                        errores.Add(new { Usuario = u.Usuario, Motivo = "El email es inválido." });
                        continue;
                    }

                    // Validar duplicados en base de datos
                    if (existentes.Any(e => e.UserName == u.Usuario))
                    {
                        errores.Add(new { Usuario = u.Usuario, Motivo = "El nombre de usuario ya existe." });
                        continue;
                    }
                    if (existentes.Any(e => e.Email == u.Email))
                    {
                        errores.Add(new { Usuario = u.Usuario, Motivo = "El correo ya existe." });
                        continue;
                    }

                    // Si tienes relación oficina, busca el IdOficina
                    var oficina = await _oficinaRepository.ObtenerPorNombre(u.NombreOficina);
                    int? oficinaId = oficina?.IdOficina;

                    // Construir nuevo usuario
                    TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                    DateTime horaPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);

                    var usuario = new ApplicationUser
                    {
                        UserName = u.Usuario,
                        Email = u.Email,
                        NombreCompleto = u.NombreCompleto,
                        IdOficina = oficinaId,
                        AuditUsuarioCreacion = User.Identity?.Name ?? string.Empty,
                        AuditFechaCreacion = DateTime.Now,
                        IpCreacion = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty,
                        Activo = true,
                        IntentosFallidosConsecutivos = 0,
                        DebeCambiarPassword = true,
                        FechaUltimoCambioPassword = horaPeru
                    };

                    // Crear usuario con clave temporal
                    var resultado = await _userManager.CreateAsync(usuario, "Admin123.");

                    if (resultado.Succeeded)
                    {
                        guardados++;
                        // actualizar cache de existentes
                        existentes.Add(new { UserName = u.Usuario, Email = u.Email });
                    }
                    else
                    {
                        var errorStr = string.Join("; ", resultado.Errors.Select(e => e.Description));
                        errores.Add(new { Usuario = u.Usuario, Motivo = errorStr });
                    }
                }
                catch (Exception ex)
                {
                    errores.Add(new { Usuario = u.Usuario, Motivo = ex.Message });
                }
            }

            return Ok(new { guardados, errores });
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ValidarExistentes([FromBody] List<string> usuarios)
        {
            if (usuarios == null || usuarios.Count == 0)
                return BadRequest("No se enviaron usuarios.");

            // Busca coincidencias en AspNetUsers por UserName
            var existentes = await _userManager.Users
                .Where(u => usuarios.Contains(u.UserName))
                .Select(u => u.UserName)
                .ToListAsync();

            return Ok(existentes); // Devuelve la lista de usuarios que ya existen
        }
    }
}
