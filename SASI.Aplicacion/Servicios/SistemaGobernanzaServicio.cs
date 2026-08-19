using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SASI.Aplicacion.Servicios
{
    public class SistemaGobernanzaServicio : ISistemaGobernanzaServicio
    {
        private readonly IGobernanzaRepository _gobernanzaRepository;
        private readonly ISistemaRepository _sistemaRepository;

        public SistemaGobernanzaServicio(
            IGobernanzaRepository gobernanzaRepository,
            ISistemaRepository sistemaRepository)
        {
            _gobernanzaRepository = gobernanzaRepository;
            _sistemaRepository = sistemaRepository;
        }

        // ----- Versiones -----

        public async Task<List<SistemaVersionDto>> ListarVersionesAsync(int sistemaId)
        {
            var versiones = await _gobernanzaRepository.ObtenerVersionesPorSistemaAsync(sistemaId);
            return versiones.Select(v => new SistemaVersionDto
            {
                IdSistemaVersion = v.IdSistemaVersion,
                SistemaId = v.SistemaId,
                Version = v.Version,
                Changelog = v.Changelog,
                Entorno = v.Entorno,
                FechaDespliegue = v.FechaDespliegue,
                UsuarioDespliegue = v.UsuarioDespliegue
            }).ToList();
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarVersionAsync(SistemaVersionDto dto, string usuario)
        {
            if (dto.SistemaId <= 0 || string.IsNullOrWhiteSpace(dto.Version))
                return (false, "El sistema y la versión son obligatorios.");

            var version = new SistemaVersion
            {
                SistemaId = dto.SistemaId,
                Version = dto.Version.Trim(),
                Changelog = dto.Changelog,
                Entorno = dto.Entorno,
                FechaDespliegue = dto.FechaDespliegue,
                UsuarioDespliegue = usuario
            };

            await _gobernanzaRepository.CrearVersionAsync(version);
            return (true, "Versión registrada correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> EditarVersionAsync(SistemaVersionDto dto, string usuario)
        {
            var existente = await _gobernanzaRepository.ObtenerVersionPorIdAsync(dto.IdSistemaVersion);
            if (existente == null)
                return (false, "La versión no existe.");

            existente.Version = dto.Version.Trim();
            existente.Changelog = dto.Changelog;
            existente.Entorno = dto.Entorno;
            existente.FechaDespliegue = dto.FechaDespliegue;
            existente.UsuarioDespliegue = usuario;

            await _gobernanzaRepository.EditarVersionAsync(existente);
            return (true, "Versión actualizada correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> EliminarVersionAsync(int id)
        {
            await _gobernanzaRepository.EliminarVersionAsync(id);
            return (true, "Versión eliminada correctamente.");
        }

        // ----- Contratos -----

        public async Task<List<SistemaContratoDto>> ListarContratosAsync(int sistemaId)
        {
            var contratos = await _gobernanzaRepository.ObtenerContratosPorSistemaAsync(sistemaId);
            return contratos.Select(c => ToContratoDto(c)).ToList();
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarContratoAsync(SistemaContratoDto dto, string usuario)
        {
            if (dto.SistemaId <= 0)
                return (false, "El sistema es obligatorio.");

            if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaFin < dto.FechaInicio)
                return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.");

            var contrato = new SistemaContrato
            {
                SistemaId = dto.SistemaId,
                Proveedor = dto.Proveedor,
                NroContrato = dto.NroContrato,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                CostoAnual = dto.CostoAnual,
                SLA_Detalle = dto.SLA_Detalle
            };

            await _gobernanzaRepository.CrearContratoAsync(contrato);
            return (true, "Contrato registrado correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> EditarContratoAsync(SistemaContratoDto dto, string usuario)
        {
            var existente = await _gobernanzaRepository.ObtenerContratoPorIdAsync(dto.IdSistemaContrato);
            if (existente == null)
                return (false, "El contrato no existe.");

            if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaFin < dto.FechaInicio)
                return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.");

            existente.Proveedor = dto.Proveedor;
            existente.NroContrato = dto.NroContrato;
            existente.FechaInicio = dto.FechaInicio;
            existente.FechaFin = dto.FechaFin;
            existente.CostoAnual = dto.CostoAnual;
            existente.SLA_Detalle = dto.SLA_Detalle;

            await _gobernanzaRepository.EditarContratoAsync(existente);
            return (true, "Contrato actualizado correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> EliminarContratoAsync(int id)
        {
            await _gobernanzaRepository.EliminarContratoAsync(id);
            return (true, "Contrato eliminado correctamente.");
        }

        // ----- Continuidad -----

        public async Task<ContinuidadDto?> ObtenerContinuidadAsync(int sistemaId)
        {
            var sistema = await _sistemaRepository.ObtenerPorId(sistemaId);
            if (sistema == null) return null;

            return new ContinuidadDto
            {
                SistemaId = sistema.IdSistema,
                RpoHoras = sistema.RpoHoras,
                RtoHoras = sistema.RtoHoras,
                PoliticaRespaldo = sistema.PoliticaRespaldo,
                FechaUltimaPruebaRestauracion = sistema.FechaUltimaPruebaRestauracion,
                DiasDesdeUltimaPrueba = sistema.FechaUltimaPruebaRestauracion.HasValue
                    ? (int)(DateTime.Today - sistema.FechaUltimaPruebaRestauracion.Value.Date).TotalDays
                    : (int?)null
            };
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarContinuidadAsync(ContinuidadDto dto, string usuario)
        {
            var sistema = await _sistemaRepository.ObtenerPorId(dto.SistemaId);
            if (sistema == null)
                return (false, "El sistema no existe.");

            sistema.RpoHoras = dto.RpoHoras;
            sistema.RtoHoras = dto.RtoHoras;
            sistema.PoliticaRespaldo = dto.PoliticaRespaldo;
            sistema.FechaUltimaPruebaRestauracion = dto.FechaUltimaPruebaRestauracion;

            await _sistemaRepository.Actualizar(sistema);
            return (true, "Métricas de continuidad actualizadas correctamente.");
        }

        // ----- Documentos -----

        public async Task<List<SistemaDocumentoDto>> ListarDocumentosAsync(int sistemaId)
        {
            var documentos = await _gobernanzaRepository.ObtenerDocumentosPorSistemaAsync(sistemaId);
            return documentos.Select(d => new SistemaDocumentoDto
            {
                IdSistemaDocumento = d.IdSistemaDocumento,
                SistemaId = d.SistemaId,
                Titulo = d.Titulo,
                TipoDoc = d.TipoDoc,
                RutaArchivo = d.RutaArchivo,
                FechaSubida = d.FechaSubida,
                UsuarioSubida = d.UsuarioSubida
            }).ToList();
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarDocumentoAsync(int sistemaId, string titulo, string tipoDoc, string rutaArchivo, string usuario)
        {
            if (sistemaId <= 0 || string.IsNullOrWhiteSpace(titulo))
                return (false, "El sistema y el título del documento son obligatorios.");

            var documento = new SistemaDocumento
            {
                SistemaId = sistemaId,
                Titulo = titulo.Trim(),
                TipoDoc = tipoDoc,
                RutaArchivo = rutaArchivo,
                FechaSubida = DateTime.Now,
                UsuarioSubida = usuario
            };

            await _gobernanzaRepository.CrearDocumentoAsync(documento);
            return (true, "Documento registrado correctamente.");
        }

        public async Task<(bool Exito, string Mensaje)> EliminarDocumentoAsync(int id)
        {
            var documento = await _gobernanzaRepository.ObtenerDocumentoPorIdAsync(id);
            if (documento != null && !string.IsNullOrEmpty(documento.RutaArchivo))
            {
                var rutaFisica = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(),
                    "wwwroot",
                    documento.RutaArchivo.TrimStart('/').Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()));

                try
                {
                    if (System.IO.File.Exists(rutaFisica))
                        System.IO.File.Delete(rutaFisica);
                }
                catch (Exception)
                {
                    // Si no se puede eliminar el archivo, no bloqueamos la eliminación del registro
                }
            }

            await _gobernanzaRepository.EliminarDocumentoAsync(id);
            return (true, "Documento eliminado correctamente.");
        }

        // ----- Helpers -----

        private static SistemaContratoDto ToContratoDto(SistemaContrato c)
        {
            var dto = new SistemaContratoDto
            {
                IdSistemaContrato = c.IdSistemaContrato,
                SistemaId = c.SistemaId,
                Proveedor = c.Proveedor,
                NroContrato = c.NroContrato,
                FechaInicio = c.FechaInicio,
                FechaFin = c.FechaFin,
                CostoAnual = c.CostoAnual,
                SLA_Detalle = c.SLA_Detalle
            };

            if (c.FechaFin.HasValue)
            {
                dto.DiasParaVencer = (int)(c.FechaFin.Value.Date - DateTime.Today).TotalDays;
                dto.EstadoContrato = dto.DiasParaVencer < 0
                    ? "Vencido"
                    : dto.DiasParaVencer <= 30
                        ? "Por vencer"
                        : "Vigente";
            }
            else
            {
                dto.EstadoContrato = "Sin fecha fin";
            }

            return dto;
        }
    }
}
