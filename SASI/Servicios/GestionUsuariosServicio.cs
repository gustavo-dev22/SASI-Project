using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SASI.Aplicacion.Servicios;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Helpers;
using SASI.Infraestructura.Identity;
using SASI.Models.Requests;
using X.PagedList;

namespace SASI.Servicios
{
    public class UsuarioDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreOficina { get; set; } = string.Empty;
    }

    public class GestionUsuariosServicio
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly ISistemaServicio _sistemaServicio;
        private readonly IRolServicio _rolServicio;
        private readonly IOficinaServicio _oficinaServicio;

        public GestionUsuariosServicio(
            UserManager<ApplicationUser> userManager,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            ISistemaServicio sistemaServicio,
            IRolServicio rolServicio,
            IOficinaServicio oficinaServicio)
        {
            _userManager = userManager;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _sistemaServicio = sistemaServicio;
            _rolServicio = rolServicio;
            _oficinaServicio = oficinaServicio;
        }

        public async Task<IPagedList<ApplicationUser>> BuscarAsync(string filtro, int page, int pageSize)
        {
            if (page < 1) page = 1;

            var query = _userManager.Users.Where(u => u.NombreCompleto.StartsWith(filtro));

            var total = await query.CountAsync();

            var usuarios = await query
                .OrderBy(u => u.NombreCompleto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new StaticPagedList<ApplicationUser>(usuarios, page, pageSize, total);
        }

        public async Task<(bool Exito, string Mensaje)> CrearAsync(NuevoUsuarioRequest dto, string usuarioCreacion, string ip)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.UserName) ||
                string.IsNullOrWhiteSpace(dto.NombreCompleto))
            {
                return (false, "Todos los campos son obligatorios.");
            }

            var existe = await _userManager.FindByEmailAsync(dto.Email);
            if (existe != null)
            {
                return (false, "El correo ya está registrado.");
            }

            var usuario = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                IdOficina = dto.OficinaId,
                AuditUsuarioCreacion = usuarioCreacion,
                AuditFechaCreacion = DateTime.Now,
                IpCreacion = ip,
                Activo = true,
                IntentosFallidosConsecutivos = 0,
                DebeCambiarPassword = true,
                FechaUltimoCambioPassword = HoraPeru(),
                LockoutEnabled = true
            };

            var resultado = await _userManager.CreateAsync(usuario, PasswordGenerator.GenerarContrasenaTemporal());

            return resultado.Succeeded
                ? (true, "Usuario registrado correctamente.")
                : (false, "Error: " + string.Join("; ", resultado.Errors.Select(e => e.Description)));
        }

        public Task<ApplicationUser?> ObtenerAsync(string id)
            => _userManager.FindByIdAsync(id);

        public async Task<(bool Exito, string Mensaje)> EditarAsync(EditarUsuarioRequest dto, string usuarioModificacion, string ip)
        {
            var usuario = await _userManager.FindByIdAsync(dto.Id);
            if (usuario == null)
                return (false, "Usuario no encontrado.");

            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.Email = dto.Email;
            usuario.IdOficina = dto.OficinaId;
            usuario.UserName = dto.UserName;
            usuario.NormalizedEmail = dto.Email.ToUpper();
            usuario.NormalizedUserName = dto.Email.ToUpper();
            usuario.Activo = dto.Activo;

            usuario.AuditUsuarioModificacion = usuarioModificacion;
            usuario.AuditFechaModificacion = DateTime.Now;
            usuario.IpModificacion = ip;

            if (dto.Bloqueado)
            {
                usuario.IntentosFallidosConsecutivos = 3;
                usuario.LockoutEnabled = true;
                usuario.LockoutEnd = DateTimeOffset.MaxValue;
            }
            else
            {
                usuario.IntentosFallidosConsecutivos = 0;
                usuario.LockoutEnabled = true;
                usuario.LockoutEnd = null;
                await _userManager.ResetAccessFailedCountAsync(usuario);
            }

            var resultado = await _userManager.UpdateAsync(usuario);
            return resultado.Succeeded
                ? (true, "Usuario actualizado correctamente.")
                : (false, string.Join("; ", resultado.Errors.Select(e => e.Description)));
        }

        public async Task<List<Sistema>> ObtenerSistemasAsync()
            => await _sistemaServicio.ListarAsync();

        public async Task<IEnumerable<Rol>> ObtenerRolesPorSistemaAsync(int sistemaId)
            => await _rolServicio.ObtenerPorSistemaIdAsync(sistemaId);

        public Task<ResultadoAsignacionUsuarioDto> AsignarSistemaARolAsync(string usuarioId, int sistemaId, int rolId, bool esPrincipal)
            => _usuarioSistemaServicio.AsignarUsuarioASistemaAsync(usuarioId, sistemaId, rolId, esPrincipal);

        public Task<List<SistemaAsignadoDto>> ObtenerSistemasPorUsuarioAsync(Guid id)
            => _usuarioSistemaServicio.ObtenerSistemasPorUsuarioAsync(id);

        public async Task<(bool Exito, string Mensaje)> QuitarSistemaAsync(Guid usuarioId, int sistemaId)
        {
            var exito = await _usuarioSistemaServicio.QuitarUsuarioDeSistemaAsync(usuarioId, sistemaId);
            return exito
                ? (true, "Sistema eliminado correctamente.")
                : (false, "No se pudo eliminar el sistema.");
        }

        public Task<ResultadoCambioEstadoDto> CambiarEstadoSistemaAsync(Guid usuarioId, int sistemaId, int rolId, bool activo)
            => _usuarioSistemaServicio.ActualizarEstadoSistemaAsync(usuarioId, sistemaId, rolId, activo);

        public Task ActualizarRolPrincipalAsync(Guid usuarioId, int sistemaId, int rolPrincipalId)
            => _usuarioSistemaServicio.ActualizarRolPrincipalAsync(usuarioId, sistemaId, rolPrincipalId);

        public async Task<(int Guardados, List<object> Errores, List<object> Credenciales)> ProcesarCargaMasivaAsync(List<UsuarioDto> usuarios, string usuarioCreacion, string ip)
        {
            int guardados = 0;
            var errores = new List<object>();
            var credenciales = new List<object>();

            var existentes = _userManager.Users
                .Select(u => new { UserName = u.UserName ?? "", Email = u.Email ?? "" })
                .ToList();

            foreach (var u in usuarios)
            {
                try
                {
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

                    var oficina = await _oficinaServicio.ObtenerPorNombreAsync(u.NombreOficina);
                    int? oficinaId = oficina?.IdOficina;

                    var usuario = new ApplicationUser
                    {
                        UserName = u.Usuario,
                        Email = u.Email,
                        NombreCompleto = u.NombreCompleto,
                        IdOficina = oficinaId,
                        AuditUsuarioCreacion = usuarioCreacion,
                        AuditFechaCreacion = DateTime.Now,
                        IpCreacion = ip,
                        Activo = true,
                        IntentosFallidosConsecutivos = 0,
                        DebeCambiarPassword = true,
                        FechaUltimoCambioPassword = HoraPeru(),
                        LockoutEnabled = true
                    };

                    var contrasenaTemporal = PasswordGenerator.GenerarContrasenaTemporal();
                    var resultado = await _userManager.CreateAsync(usuario, contrasenaTemporal);

                    if (resultado.Succeeded)
                    {
                        guardados++;
                        credenciales.Add(new { Usuario = u.Usuario, ContrasenaTemporal = contrasenaTemporal });
                        existentes.Add(new { UserName = u.Usuario, Email = u.Email });
                    }
                    else
                    {
                        var errorStr = string.Join("; ", resultado.Errors.Select(e => e.Description));
                        errores.Add(new { Usuario = u.Usuario, Motivo = errorStr });
                    }
                }
                catch (Exception)
                {
                    errores.Add(new { Usuario = u.Usuario, Motivo = "Error inesperado al procesar el registro." });
                }
            }

            return (guardados, errores, credenciales);
        }

        public async Task<List<string>> ValidarExistentesAsync(List<string> usuarios)
        {
            return await _userManager.Users
                .Where(u => usuarios.Contains(u.UserName ?? ""))
                .Select(u => u.UserName ?? "")
                .ToListAsync();
        }

        private static bool IsValidEmail(string email)
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

        private static DateTime HoraPeru()
        {
            TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);
        }
    }
}
