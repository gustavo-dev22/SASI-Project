using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface IOperacionSoporteRepository
    {
        // Incidencias
        Task<List<Incidencia>> ObtenerIncidenciasPorSistemaAsync(int sistemaId);
        Task<List<Incidencia>> ObtenerIncidenciasAsync();
        Task<Incidencia?> ObtenerIncidenciaPorIdAsync(int id);
        Task CrearIncidenciaAsync(Incidencia incidencia);
        Task EditarIncidenciaAsync(Incidencia incidencia);
        Task CambiarEstadoIncidenciaAsync(int id, EstadoIncidencia nuevoEstado, string? usuario);
        Task EliminarIncidenciaAsync(int id);

        // Solicitudes de acceso
        Task<List<SolicitudAcceso>> ObtenerSolicitudesAsync(EstadoSolicitudAcceso? estado = null);
        Task<SolicitudAcceso?> ObtenerSolicitudPorIdAsync(int id);
        Task CrearSolicitudAsync(SolicitudAcceso solicitud);
        Task ResponderSolicitudAsync(SolicitudAcceso solicitud);

        // Estado operativo
        Task<List<EstadoOperativoSistema>> ObtenerHistorialEstadoAsync(int sistemaId);
        Task RegistrarEstadoOperativoAsync(EstadoOperativoSistema estado);
    }
}
