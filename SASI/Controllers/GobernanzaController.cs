using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Authorization;
using SASI.Dominio.DTO;
using SASI.Models.Requests;
using SASI.Servicios;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class GobernanzaController : Controller
    {
        private readonly ISistemaGobernanzaServicio _gobernanzaServicio;
        private readonly ISistemaServicio _sistemaServicio;

        public GobernanzaController(
            ISistemaGobernanzaServicio gobernanzaServicio,
            ISistemaServicio sistemaServicio)
        {
            _gobernanzaServicio = gobernanzaServicio;
            _sistemaServicio = sistemaServicio;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Ficha(int sistemaId)
        {
            var sistema = await _sistemaServicio.ObtenerPorIdAsync(sistemaId);
            if (sistema == null) return NotFound();

            var versiones = await _gobernanzaServicio.ListarVersionesAsync(sistemaId);
            var contratos = await _gobernanzaServicio.ListarContratosAsync(sistemaId);
            var continuidad = await _gobernanzaServicio.ObtenerContinuidadAsync(sistemaId);
            var documentos = await _gobernanzaServicio.ListarDocumentosAsync(sistemaId);

            ViewBag.Sistema = sistema;
            ViewBag.SistemaId = sistemaId;
            ViewBag.Versiones = versiones;
            ViewBag.Contratos = contratos;
            ViewBag.Continuidad = continuidad ?? new ContinuidadDto { SistemaId = sistemaId };
            ViewBag.Documentos = documentos;

            return View();
        }

        // ===================== VERSIONES =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Versiones(int sistemaId)
        {
            var versiones = await _gobernanzaServicio.ListarVersionesAsync(sistemaId);
            return Json(versiones);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarVersion([FromForm] SistemaVersionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var dto = new SistemaVersionDto
            {
                IdSistemaVersion = request.IdSistemaVersion,
                SistemaId = request.SistemaId,
                Version = request.Version,
                Changelog = request.Changelog,
                Entorno = request.Entorno,
                FechaDespliegue = request.FechaDespliegue
            };

            var resultado = request.IdSistemaVersion > 0
                ? await _gobernanzaServicio.EditarVersionAsync(dto, User.Identity?.Name ?? "")
                : await _gobernanzaServicio.RegistrarVersionAsync(dto, User.Identity?.Name ?? "");

            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarVersion(int id)
        {
            var resultado = await _gobernanzaServicio.EliminarVersionAsync(id);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        // ===================== CONTRATOS =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Contratos(int sistemaId)
        {
            var contratos = await _gobernanzaServicio.ListarContratosAsync(sistemaId);
            return Json(contratos);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarContrato([FromForm] SistemaContratoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var dto = new SistemaContratoDto
            {
                IdSistemaContrato = request.IdSistemaContrato,
                SistemaId = request.SistemaId,
                Proveedor = request.Proveedor,
                NroContrato = request.NroContrato,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin,
                CostoAnual = request.CostoAnual,
                SLA_Detalle = request.SLA_Detalle
            };

            var resultado = request.IdSistemaContrato > 0
                ? await _gobernanzaServicio.EditarContratoAsync(dto, User.Identity?.Name ?? "")
                : await _gobernanzaServicio.RegistrarContratoAsync(dto, User.Identity?.Name ?? "");

            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarContrato(int id)
        {
            var resultado = await _gobernanzaServicio.EliminarContratoAsync(id);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        // ===================== CONTINUIDAD =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Continuidad(int sistemaId)
        {
            var continuidad = await _gobernanzaServicio.ObtenerContinuidadAsync(sistemaId);
            return Json(continuidad);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarContinuidad([FromForm] ContinuidadRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var dto = new ContinuidadDto
            {
                SistemaId = request.SistemaId,
                RpoHoras = request.RpoHoras,
                RtoHoras = request.RtoHoras,
                PoliticaRespaldo = request.PoliticaRespaldo,
                FechaUltimaPruebaRestauracion = request.FechaUltimaPruebaRestauracion
            };

            var resultado = await _gobernanzaServicio.ActualizarContinuidadAsync(dto, User.Identity?.Name ?? "");
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        // ===================== DOCUMENTOS =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Documentos(int sistemaId)
        {
            var documentos = await _gobernanzaServicio.ListarDocumentosAsync(sistemaId);
            return Json(documentos);
        }

        [HttpPost]
        public async Task<IActionResult> SubirDocumento(int sistemaId, string titulo, string tipoDoc, IFormFile? archivo)
        {
            if (sistemaId <= 0 || string.IsNullOrWhiteSpace(titulo))
                return BadRequest(new { success = false, mensaje = "El sistema y el título del documento son obligatorios." });

            if (string.IsNullOrWhiteSpace(tipoDoc))
                tipoDoc = "Manual";

            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { success = false, mensaje = "Debe seleccionar un archivo." });

            var extension = System.IO.Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var permitidas = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg" };
            if (!permitidas.Contains(extension))
                return BadRequest(new { success = false, mensaje = "Tipo de archivo no permitido." });

            var rutaRelativa = $"uploads/sistemas/{sistemaId}";
            var carpetaFisica = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                "wwwroot",
                rutaRelativa.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()));

            System.IO.Directory.CreateDirectory(carpetaFisica);

            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
            var rutaCompleta = System.IO.Path.Combine(carpetaFisica, nombreArchivo);

            using (var stream = System.IO.File.Create(rutaCompleta))
            {
                await archivo.CopyToAsync(stream);
            }

            var resultado = await _gobernanzaServicio.RegistrarDocumentoAsync(
                sistemaId,
                titulo,
                tipoDoc,
                $"{rutaRelativa}/{nombreArchivo}",
                User.Identity?.Name ?? "");

            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDocumento(int id)
        {
            var resultado = await _gobernanzaServicio.EliminarDocumentoAsync(id);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }
    }
}
