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
    public class GobernanzaRepository : IGobernanzaRepository
    {
        private readonly SasiDbContext _context;

        public GobernanzaRepository(SasiDbContext context)
        {
            _context = context;
        }

        // ----- Versiones -----

        public async Task<List<SistemaVersion>> ObtenerVersionesPorSistemaAsync(int sistemaId)
            => await _context.SistemaVersiones
                .Where(v => v.SistemaId == sistemaId)
                .OrderByDescending(v => v.FechaDespliegue)
                .ThenByDescending(v => v.IdSistemaVersion)
                .ToListAsync();

        public async Task<SistemaVersion?> ObtenerVersionPorIdAsync(int id)
            => await _context.SistemaVersiones.FindAsync(id);

        public async Task CrearVersionAsync(SistemaVersion version)
        {
            _context.SistemaVersiones.Add(version);
            await _context.SaveChangesAsync();
        }

        public async Task EditarVersionAsync(SistemaVersion version)
        {
            var existente = await _context.SistemaVersiones.FindAsync(version.IdSistemaVersion);
            if (existente == null) return;

            existente.Version = version.Version;
            existente.Changelog = version.Changelog;
            existente.Entorno = version.Entorno;
            existente.FechaDespliegue = version.FechaDespliegue;
            existente.UsuarioDespliegue = version.UsuarioDespliegue;

            await _context.SaveChangesAsync();
        }

        public async Task EliminarVersionAsync(int id)
        {
            var version = await _context.SistemaVersiones.FindAsync(id);
            if (version == null) return;

            _context.SistemaVersiones.Remove(version);
            await _context.SaveChangesAsync();
        }

        // ----- Contratos -----

        public async Task<List<SistemaContrato>> ObtenerContratosPorSistemaAsync(int sistemaId)
            => await _context.SistemaContratos
                .Where(c => c.SistemaId == sistemaId)
                .OrderBy(c => c.FechaFin)
                .ToListAsync();

        public async Task<SistemaContrato?> ObtenerContratoPorIdAsync(int id)
            => await _context.SistemaContratos.FindAsync(id);

        public async Task CrearContratoAsync(SistemaContrato contrato)
        {
            _context.SistemaContratos.Add(contrato);
            await _context.SaveChangesAsync();
        }

        public async Task EditarContratoAsync(SistemaContrato contrato)
        {
            var existente = await _context.SistemaContratos.FindAsync(contrato.IdSistemaContrato);
            if (existente == null) return;

            existente.Proveedor = contrato.Proveedor;
            existente.NroContrato = contrato.NroContrato;
            existente.FechaInicio = contrato.FechaInicio;
            existente.FechaFin = contrato.FechaFin;
            existente.CostoAnual = contrato.CostoAnual;
            existente.SLA_Detalle = contrato.SLA_Detalle;

            await _context.SaveChangesAsync();
        }

        public async Task EliminarContratoAsync(int id)
        {
            var contrato = await _context.SistemaContratos.FindAsync(id);
            if (contrato == null) return;

            _context.SistemaContratos.Remove(contrato);
            await _context.SaveChangesAsync();
        }

        // ----- Documentos -----

        public async Task<List<SistemaDocumento>> ObtenerDocumentosPorSistemaAsync(int sistemaId)
            => await _context.SistemaDocumentos
                .Where(d => d.SistemaId == sistemaId)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

        public async Task<SistemaDocumento?> ObtenerDocumentoPorIdAsync(int id)
            => await _context.SistemaDocumentos.FindAsync(id);

        public async Task CrearDocumentoAsync(SistemaDocumento documento)
        {
            _context.SistemaDocumentos.Add(documento);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarDocumentoAsync(int id)
        {
            var documento = await _context.SistemaDocumentos.FindAsync(id);
            if (documento == null) return;

            _context.SistemaDocumentos.Remove(documento);
            await _context.SaveChangesAsync();
        }
    }
}
