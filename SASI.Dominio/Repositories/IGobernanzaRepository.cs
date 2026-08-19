using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface IGobernanzaRepository
    {
        // Versiones / despliegues
        Task<List<SistemaVersion>> ObtenerVersionesPorSistemaAsync(int sistemaId);
        Task<SistemaVersion?> ObtenerVersionPorIdAsync(int id);
        Task CrearVersionAsync(SistemaVersion version);
        Task EditarVersionAsync(SistemaVersion version);
        Task EliminarVersionAsync(int id);

        // Contratos
        Task<List<SistemaContrato>> ObtenerContratosPorSistemaAsync(int sistemaId);
        Task<SistemaContrato?> ObtenerContratoPorIdAsync(int id);
        Task CrearContratoAsync(SistemaContrato contrato);
        Task EditarContratoAsync(SistemaContrato contrato);
        Task EliminarContratoAsync(int id);

        // Documentos
        Task<List<SistemaDocumento>> ObtenerDocumentosPorSistemaAsync(int sistemaId);
        Task<SistemaDocumento?> ObtenerDocumentoPorIdAsync(int id);
        Task CrearDocumentoAsync(SistemaDocumento documento);
        Task EliminarDocumentoAsync(int id);
    }
}
