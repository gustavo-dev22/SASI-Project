using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Helpers;
using SASI.Infraestructura.Identity;
using SistemaConvocatorias.Infraestructura.Datos;

namespace SASI.Servicios
{
    public interface ISolicitudPasswordServicio
    {
        Task CrearSolicitudAsync(string identificador, string? mensaje, string? clientId, int? sistemaId);
        Task<List<SolicitudPasswordDto>> ListarAsync(EstadoSolicitudPassword? estado = null);
        Task<int> ContarPendientesAsync();
        Task<(bool Exito, string Mensaje, string? ContrasenaTemporal)> RestablecerUsuarioAsync(Guid usuarioId, string admin);
        Task<(bool Exito, string Mensaje, string? ContrasenaTemporal)> RestablecerSolicitudAsync(int idSolicitud, string admin);
        Task<(bool Exito, string Mensaje)> RechazarSolicitudAsync(int idSolicitud, string admin, string? comentario);
    }

    // Recuperación de contraseña asistida por administrador.
    // El usuario deja una solicitud desde el login; un administrador la atiende y
    // entrega una contraseña temporal (el usuario la cambia al ingresar).
    public class SolicitudPasswordServicio : ISolicitudPasswordServicio
    {
        private readonly SasiDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SolicitudPasswordServicio> _logger;

        public SolicitudPasswordServicio(
            SasiDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<SolicitudPasswordServicio> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task CrearSolicitudAsync(string identificador, string? mensaje, string? clientId, int? sistemaId)
        {
            identificador = (identificador ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identificador))
                return;

            // Acepta correo o nombre de usuario.
            var usuario = await _userManager.FindByEmailAsync(identificador);
            if (usuario == null)
                usuario = await _userManager.FindByNameAsync(identificador);

            // No se registra nada si el usuario no existe (evita enumeración y spam).
            if (usuario == null)
                return;

            // Evita duplicados pendientes recientes para el mismo usuario.
            var desde = DateTime.Now.AddMinutes(-10);
            var yaExiste = await _db.SolicitudesPassword.AnyAsync(s =>
                s.UsuarioId == usuario.Id &&
                s.Estado == EstadoSolicitudPassword.Pendiente &&
                s.FechaSolicitud >= desde);

            if (yaExiste)
                return;

            _db.SolicitudesPassword.Add(new SolicitudRestablecimientoPassword
            {
                UsuarioId = usuario.Id,
                Identificador = identificador,
                UserName = usuario.UserName,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                SistemaId = sistemaId,
                ClientId = clientId,
                Mensaje = string.IsNullOrWhiteSpace(mensaje) ? null : mensaje.Trim(),
                Estado = EstadoSolicitudPassword.Pendiente,
                FechaSolicitud = DateTime.Now
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Solicitud de restablecimiento de contraseña registrada para {Usuario} (sistema {SistemaId}).",
                usuario.UserName, sistemaId);
        }

        public async Task<List<SolicitudPasswordDto>> ListarAsync(EstadoSolicitudPassword? estado = null)
        {
            var query = _db.SolicitudesPassword.AsNoTracking();

            if (estado.HasValue)
                query = query.Where(s => s.Estado == estado.Value);

            var lista = await query
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            var sistemas = await _db.Sistemas.AsNoTracking()
                .ToDictionaryAsync(s => s.IdSistema, s => s.Nombre);

            return lista.Select(s => new SolicitudPasswordDto
            {
                IdSolicitud = s.IdSolicitud,
                UsuarioId = s.UsuarioId,
                Identificador = s.Identificador,
                UserName = s.UserName,
                Email = s.Email,
                NombreCompleto = s.NombreCompleto,
                SistemaNombre = s.SistemaId.HasValue && sistemas.TryGetValue(s.SistemaId.Value, out var nombre) ? nombre : null,
                ClientId = s.ClientId,
                Mensaje = s.Mensaje,
                Estado = s.Estado,
                FechaSolicitud = s.FechaSolicitud,
                FechaRespuesta = s.FechaRespuesta,
                AtendidoPor = s.AtendidoPor,
                ComentarioRespuesta = s.ComentarioRespuesta
            }).ToList();
        }

        public Task<int> ContarPendientesAsync()
            => _db.SolicitudesPassword.CountAsync(s => s.Estado == EstadoSolicitudPassword.Pendiente);

        public async Task<(bool Exito, string Mensaje, string? ContrasenaTemporal)> RestablecerUsuarioAsync(Guid usuarioId, string admin)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
            if (usuario == null)
                return (false, "Usuario no encontrado.", null);

            var temporal = PasswordGenerator.GenerarContrasenaTemporal();
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await _userManager.ResetPasswordAsync(usuario, token, temporal);

            if (!resultado.Succeeded)
            {
                var error = string.Join("; ", resultado.Errors.Select(e => e.Description));
                _logger.LogWarning("No se pudo restablecer la contraseña de {Usuario}: {Error}", usuario.UserName, error);
                return (false, error, null);
            }

            // Desbloqueo y control de cambio obligatorio al primer ingreso.
            usuario.DebeCambiarPassword = true;
            usuario.FechaUltimoCambioPassword = DateTime.Now;
            usuario.IntentosFallidosConsecutivos = 0;
            usuario.AccessFailedCount = 0;
            usuario.LockoutEnabled = true;
            usuario.LockoutEnd = null;
            usuario.AuditUsuarioModificacion = admin;
            usuario.AuditFechaModificacion = DateTime.Now;

            await _userManager.UpdateAsync(usuario);

            _logger.LogWarning(
                "Contraseña restablecida para {Usuario} por {Admin}. Se desbloqueó la cuenta.",
                usuario.UserName, admin);

            return (true, "Contraseña restablecida correctamente.", temporal);
        }

        public async Task<(bool Exito, string Mensaje, string? ContrasenaTemporal)> RestablecerSolicitudAsync(int idSolicitud, string admin)
        {
            var solicitud = await _db.SolicitudesPassword
                .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud);

            if (solicitud == null || solicitud.Estado != EstadoSolicitudPassword.Pendiente)
                return (false, "La solicitud no existe o ya fue atendida.", null);

            if (!solicitud.UsuarioId.HasValue)
                return (false, "La solicitud no tiene un usuario asociado.", null);

            var resultado = await RestablecerUsuarioAsync(solicitud.UsuarioId.Value, admin);
            if (!resultado.Exito)
                return resultado;

            solicitud.Estado = EstadoSolicitudPassword.Atendida;
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.AtendidoPor = admin;
            solicitud.ComentarioRespuesta = "Contraseña restablecida por administrador.";
            await _db.SaveChangesAsync();

            return resultado;
        }

        public async Task<(bool Exito, string Mensaje)> RechazarSolicitudAsync(int idSolicitud, string admin, string? comentario)
        {
            var solicitud = await _db.SolicitudesPassword
                .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud);

            if (solicitud == null || solicitud.Estado != EstadoSolicitudPassword.Pendiente)
                return (false, "La solicitud no existe o ya fue atendida.");

            solicitud.Estado = EstadoSolicitudPassword.Rechazada;
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.AtendidoPor = admin;
            solicitud.ComentarioRespuesta = string.IsNullOrWhiteSpace(comentario)
                ? "Solicitud rechazada por administrador."
                : comentario.Trim();

            await _db.SaveChangesAsync();
            return (true, "Solicitud rechazada.");
        }
    }
}
