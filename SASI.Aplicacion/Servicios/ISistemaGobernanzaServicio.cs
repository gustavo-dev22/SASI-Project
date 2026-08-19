using SASI.Dominio.DTO;

namespace SASI.Aplicacion.Servicios
{
    public interface ISistemaGobernanzaServicio
    {
        // Versiones / despliegues
        Task<List<SistemaVersionDto>> ListarVersionesAsync(int sistemaId);
        Task<(bool Exito, string Mensaje)> RegistrarVersionAsync(SistemaVersionDto dto, string usuario);
        Task<(bool Exito, string Mensaje)> EditarVersionAsync(SistemaVersionDto dto, string usuario);
        Task<(bool Exito, string Mensaje)> EliminarVersionAsync(int id);

        // Contratos
        Task<List<SistemaContratoDto>> ListarContratosAsync(int sistemaId);
        Task<(bool Exito, string Mensaje)> RegistrarContratoAsync(SistemaContratoDto dto, string usuario);
        Task<(bool Exito, string Mensaje)> EditarContratoAsync(SistemaContratoDto dto, string usuario);
        Task<(bool Exito, string Mensaje)> EliminarContratoAsync(int id);

        // Continuidad (RPO/RTO + prueba de restauración)
        Task<ContinuidadDto?> ObtenerContinuidadAsync(int sistemaId);
        Task<(bool Exito, string Mensaje)> ActualizarContinuidadAsync(ContinuidadDto dto, string usuario);

        // Documentos
        Task<List<SistemaDocumentoDto>> ListarDocumentosAsync(int sistemaId);
        Task<(bool Exito, string Mensaje)> RegistrarDocumentoAsync(int sistemaId, string titulo, string tipoDoc, string rutaArchivo, string usuario);
        Task<(bool Exito, string Mensaje)> EliminarDocumentoAsync(int id);
    }
}
