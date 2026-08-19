using Microsoft.AspNetCore.Identity;
using SASI.Aplicacion.Servicios;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SASI.Servicios
{
    public interface ISoporteServicio
    {
        // Incidencias
        Task<List<IncidenciaDto>> ListarIncidenciasAsync(int? sistemaId = null);
        Task<(bool Exito, string Mensaje)> RegistrarIncidenciaAsync(IncidenciaDto dto, string usuario);
        Task<(bool Exito, string Mensaje)> EditarIncidenciaAsync(IncidenciaDto dto, string usuario);
        Task<(bool Exito, string Mensaje)> CambiarEstadoIncidenciaAsync(int id, EstadoIncidencia estado, string usuario);
        Task<(bool Exito, string Mensaje)> EliminarIncidenciaAsync(int id);

        // Solicitudes de acceso
        Task<List<SolicitudAccesoDto>> ListarSolicitudesAsync(EstadoSolicitudAcceso? estado = null);
        Task<(bool Exito, string Mensaje)> CrearSolicitudAsync(int sistemaId, int rolId, string justificacion, Guid usuarioId);
        Task<(bool Exito, string Mensaje)> AprobarSolicitudAsync(int idSolicitud, string aprobadoPor, string? comentario);
        Task<(bool Exito, string Mensaje)> RechazarSolicitudAsync(int idSolicitud, string aprobadoPor, string? comentario);

        // Estado operativo
        Task<List<EstadoOperativoDto>> ObtenerHistorialEstadoAsync(int sistemaId);
        Task<(bool Exito, string Mensaje)> CambiarEstadoOperativoAsync(int sistemaId, EstadoOperativo estado, string? observacion, string usuario);
    }

    public class SoporteServicio : ISoporteServicio
    {
        private readonly IOperacionSoporteRepository _repo;
        private readonly ISistemaRepository _sistemaRepository;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly UserManager<ApplicationUser> _userManager;

        public SoporteServicio(
            IOperacionSoporteRepository repo,
            ISistemaRepository sistemaRepository,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            UserManager<ApplicationUser> userManager)
        {
            _repo = repo;
            _sistemaRepository = sistemaRepository;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _userManager = userManager;
        }

        // ----- Incidencias -----

        public async Task<List<IncidenciaDto>> ListarIncidenciasAsync(int? sistemaId = null)
        {
            var incidencias = sistemaId.HasValue
                ? await _repo.ObtenerIncidenciasPorSistemaAsync(sistemaId.Value)
                : await _repo.ObtenerIncidenciasAsync();

            return incidencias.Select(i => new IncidenciaDto
            {
                IdIncidencia = i.IdIncidencia,
                SistemaId = i.SistemaId,
                NombreSistema = i.Sistema?.Nombre ?? "",
                Titulo = i.Titulo,
                Descripcion = i.Descripcion,
                Prioridad = i.Prioridad,
                Estado = i.Estado,
                Responsable = i.Responsable,
                FechaReporte = i.FechaReporte,
                FechaAtencion = i.FechaAtencion,
                FechaCierre = i.FechaCierre,
                UsuarioReporte = i.UsuarioReporte,
                TiempoAtencionHoras = i.FechaCierre.HasValue
                    ? (i.FechaCierre.Value - i.FechaReporte).TotalHours
                    : (i.FechaAtencion.HasValue ? (i.FechaAtencion.Value - i.FechaReporte).TotalHours : (double?)null)
            }).ToList();
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarIncidenciaAsync(IncidenciaDto dto, string usuario)
        {
            if (dto.SistemaId <= 0 || string.IsNullOrWhiteSpace(dto.Titulo))
                return (false, "El sistema y el título son obligatorios.");

            var incidencia = new Incidencia
            {
                SistemaId = dto.SistemaId,
                Titulo = dto.Titulo.Trim(),
                Descripcion = dto.Descripcion ?? "",
                Prioridad = dto.Prioridad,
                Estado = EstadoIncidencia.Abierta,
                Responsable = dto.Responsable,
                FechaReporte = DateTime.Now,
                UsuarioReporte = usuario
            };

            await _repo.CrearIncidenciaAsync(incidencia);
            return (true, "Incidencia registrada correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> EditarIncidenciaAsync(IncidenciaDto dto, string usuario)
        {
            var existente = await _repo.ObtenerIncidenciaPorIdAsync(dto.IdIncidencia);
            if (existente == null)
                return (false, "La incidencia no existe.");

            existente.Titulo = dto.Titulo.Trim();
            existente.Descripcion = dto.Descripcion ?? "";
            existente.Prioridad = dto.Prioridad;
            existente.Estado = dto.Estado;
            existente.Responsable = dto.Responsable;

            if (dto.Estado == EstadoIncidencia.EnProceso && !existente.FechaAtencion.HasValue)
                existente.FechaAtencion = DateTime.Now;
            if ((dto.Estado == EstadoIncidencia.Resuelta || dto.Estado == EstadoIncidencia.Cerrada) && !existente.FechaCierre.HasValue)
                existente.FechaCierre = DateTime.Now;

            await _repo.EditarIncidenciaAsync(existente);
            return (true, "Incidencia actualizada correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> CambiarEstadoIncidenciaAsync(int id, EstadoIncidencia estado, string usuario)
        {
            await _repo.CambiarEstadoIncidenciaAsync(id, estado, usuario);
            return (true, "Estado de la incidencia actualizado.");
        }

        public async Task<(bool Exito, string Mensaje)> EliminarIncidenciaAsync(int id)
        {
            await _repo.EliminarIncidenciaAsync(id);
            return (true, "Incidencia eliminada correctamente.");
        }

        // ----- Solicitudes de acceso -----

        public async Task<List<SolicitudAccesoDto>> ListarSolicitudesAsync(EstadoSolicitudAcceso? estado = null)
        {
            var solicitudes = await _repo.ObtenerSolicitudesAsync(estado);
            var resultado = new System.Collections.Generic.List<SolicitudAccesoDto>();

            foreach (var s in solicitudes)
            {
                var usuario = await _userManager.FindByIdAsync(s.UsuarioId.ToString());

                resultado.Add(new SolicitudAccesoDto
                {
                    IdSolicitud = s.IdSolicitud,
                    UsuarioId = s.UsuarioId,
                    EmailUsuario = usuario?.Email,
                    NombreUsuario = usuario?.NombreCompleto,
                    SistemaId = s.SistemaId,
                    NombreSistema = s.Sistema?.Nombre ?? "",
                    RolId = s.RolId,
                    NombreRol = s.Rol?.Nombre ?? "",
                    Justificacion = s.Justificacion,
                    Estado = s.Estado,
                    FechaSolicitud = s.FechaSolicitud,
                    FechaRespuesta = s.FechaRespuesta,
                    AprobadoPor = s.AprobadoPor,
                    ComentarioRespuesta = s.ComentarioRespuesta
                });
            }

            return resultado;
        }

        public async Task<(bool Exito, string Mensaje)> CrearSolicitudAsync(int sistemaId, int rolId, string justificacion, Guid usuarioId)
        {
            if (sistemaId <= 0 || rolId <= 0)
                return (false, "Debe seleccionar sistema y rol.");

            var yaAsignado = await _usuarioSistemaServicio.UsuarioTieneRolActivoEnSistemaAsync(usuarioId, sistemaId);
            if (yaAsignado)
                return (false, "El usuario ya tiene un rol activo en este sistema.");

            var solicitud = new SolicitudAcceso
            {
                UsuarioId = usuarioId,
                SistemaId = sistemaId,
                RolId = rolId,
                Justificacion = justificacion,
                Estado = EstadoSolicitudAcceso.Pendiente,
                FechaSolicitud = DateTime.Now
            };

            await _repo.CrearSolicitudAsync(solicitud);
            return (true, "Solicitud de acceso registrada. Un administrador debe aprobarla.");
        }

        public async Task<(bool Exito, string Mensaje)> AprobarSolicitudAsync(int idSolicitud, string aprobadoPor, string? comentario)
        {
            var solicitud = await _repo.ObtenerSolicitudPorIdAsync(idSolicitud);
            if (solicitud == null)
                return (false, "La solicitud no existe.");

            if (solicitud.Estado != EstadoSolicitudAcceso.Pendiente)
                return (false, "La solicitud ya fue respondida.");

            // Asignar el sistema/rol al usuario
            var asignacion = await _usuarioSistemaServicio.AsignarUsuarioASistemaAsync(
                solicitud.UsuarioId.ToString(),
                solicitud.SistemaId,
                solicitud.RolId,
                esPrincipal: false);

            if (!asignacion.Exito)
                return (false, asignacion.Mensaje);

            solicitud.Estado = EstadoSolicitudAcceso.Aprobada;
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.AprobadoPor = aprobadoPor;
            solicitud.ComentarioRespuesta = comentario;

            await _repo.ResponderSolicitudAsync(solicitud);
            return (true, "Solicitud aprobada y acceso asignado al usuario.");
        }

        public async Task<(bool Exito, string Mensaje)> RechazarSolicitudAsync(int idSolicitud, string aprobadoPor, string? comentario)
        {
            var solicitud = await _repo.ObtenerSolicitudPorIdAsync(idSolicitud);
            if (solicitud == null)
                return (false, "La solicitud no existe.");

            if (solicitud.Estado != EstadoSolicitudAcceso.Pendiente)
                return (false, "La solicitud ya fue respondida.");

            solicitud.Estado = EstadoSolicitudAcceso.Rechazada;
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.AprobadoPor = aprobadoPor;
            solicitud.ComentarioRespuesta = comentario;

            await _repo.ResponderSolicitudAsync(solicitud);
            return (true, "Solicitud rechazada.");
        }

        // ----- Estado operativo -----

        public async Task<List<EstadoOperativoDto>> ObtenerHistorialEstadoAsync(int sistemaId)
        {
            var historial = await _repo.ObtenerHistorialEstadoAsync(sistemaId);
            return historial.Select(e => new EstadoOperativoDto
            {
                IdEstadoOperativo = e.IdEstadoOperativo,
                SistemaId = e.SistemaId,
                Estado = e.Estado,
                Observacion = e.Observacion,
                FechaRegistro = e.FechaRegistro,
                UsuarioRegistro = e.UsuarioRegistro
            }).ToList();
        }

        public async Task<(bool Exito, string Mensaje)> CambiarEstadoOperativoAsync(int sistemaId, EstadoOperativo estado, string? observacion, string usuario)
        {
            var sistema = await _sistemaRepository.ObtenerPorId(sistemaId);
            if (sistema == null)
                return (false, "El sistema no existe.");

            // Registrar historial
            var registro = new EstadoOperativoSistema
            {
                SistemaId = sistemaId,
                Estado = estado,
                Observacion = observacion,
                FechaRegistro = DateTime.Now,
                UsuarioRegistro = usuario
            };

            await _repo.RegistrarEstadoOperativoAsync(registro);

            // Actualizar indicador actual del sistema
            sistema.EstadoOperativoActual = estado;
            await _sistemaRepository.Actualizar(sistema);

            return (true, $"Estado operativo actualizado a {estado}.");
        }
    }
}
