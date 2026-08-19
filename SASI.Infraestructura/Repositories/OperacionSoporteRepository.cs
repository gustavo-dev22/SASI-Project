using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SistemaConvocatorias.Infraestructura.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class OperacionSoporteRepository : IOperacionSoporteRepository
    {
        private readonly SasiDbContext _context;

        public OperacionSoporteRepository(SasiDbContext context)
        {
            _context = context;
        }

        // ----- Incidencias -----

        public async Task<List<Incidencia>> ObtenerIncidenciasPorSistemaAsync(int sistemaId)
            => await _context.Incidencias
                .Include(i => i.Sistema)
                .Where(i => i.SistemaId == sistemaId)
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();

        public async Task<List<Incidencia>> ObtenerIncidenciasAsync()
            => await _context.Incidencias
                .Include(i => i.Sistema)
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();

        public async Task<Incidencia?> ObtenerIncidenciaPorIdAsync(int id)
            => await _context.Incidencias.FindAsync(id);

        public async Task CrearIncidenciaAsync(Incidencia incidencia)
        {
            _context.Incidencias.Add(incidencia);
            await _context.SaveChangesAsync();
        }

        public async Task EditarIncidenciaAsync(Incidencia incidencia)
        {
            var existente = await _context.Incidencias.FindAsync(incidencia.IdIncidencia);
            if (existente == null) return;

            existente.Titulo = incidencia.Titulo;
            existente.Descripcion = incidencia.Descripcion;
            existente.Prioridad = incidencia.Prioridad;
            existente.Responsable = incidencia.Responsable;
            existente.Estado = incidencia.Estado;
            existente.FechaAtencion = incidencia.FechaAtencion;
            existente.FechaCierre = incidencia.FechaCierre;

            await _context.SaveChangesAsync();
        }

        public async Task CambiarEstadoIncidenciaAsync(int id, EstadoIncidencia nuevoEstado, string? usuario)
        {
            var incidencia = await _context.Incidencias.FindAsync(id);
            if (incidencia == null) return;

            incidencia.Estado = nuevoEstado;
            if (nuevoEstado == EstadoIncidencia.EnProceso && !incidencia.FechaAtencion.HasValue)
                incidencia.FechaAtencion = DateTime.Now;
            if (nuevoEstado == EstadoIncidencia.Resuelta || nuevoEstado == EstadoIncidencia.Cerrada)
                incidencia.FechaCierre = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task EliminarIncidenciaAsync(int id)
        {
            var incidencia = await _context.Incidencias.FindAsync(id);
            if (incidencia == null) return;

            _context.Incidencias.Remove(incidencia);
            await _context.SaveChangesAsync();
        }

        // ----- Solicitudes de acceso -----

        public async Task<List<SolicitudAcceso>> ObtenerSolicitudesAsync(EstadoSolicitudAcceso? estado = null)
        {
            var query = _context.SolicitudesAcceso
                .Include(s => s.Sistema)
                .Include(s => s.Rol)
                .AsQueryable();

            if (estado.HasValue)
                query = query.Where(s => s.Estado == estado.Value);

            return await query
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<SolicitudAcceso?> ObtenerSolicitudPorIdAsync(int id)
            => await _context.SolicitudesAcceso
                .Include(s => s.Sistema)
                .Include(s => s.Rol)
                .FirstOrDefaultAsync(s => s.IdSolicitud == id);

        public async Task CrearSolicitudAsync(SolicitudAcceso solicitud)
        {
            _context.SolicitudesAcceso.Add(solicitud);
            await _context.SaveChangesAsync();
        }

        public async Task ResponderSolicitudAsync(SolicitudAcceso solicitud)
        {
            var existente = await _context.SolicitudesAcceso.FindAsync(solicitud.IdSolicitud);
            if (existente == null) return;

            existente.Estado = solicitud.Estado;
            existente.FechaRespuesta = solicitud.FechaRespuesta;
            existente.AprobadoPor = solicitud.AprobadoPor;
            existente.ComentarioRespuesta = solicitud.ComentarioRespuesta;

            await _context.SaveChangesAsync();
        }

        // ----- Estado operativo -----

        public async Task<List<EstadoOperativoSistema>> ObtenerHistorialEstadoAsync(int sistemaId)
            => await _context.EstadosOperativos
                .Where(e => e.SistemaId == sistemaId)
                .OrderByDescending(e => e.FechaRegistro)
                .ToListAsync();

        public async Task RegistrarEstadoOperativoAsync(EstadoOperativoSistema estado)
        {
            _context.EstadosOperativos.Add(estado);
            await _context.SaveChangesAsync();
        }
    }
}
