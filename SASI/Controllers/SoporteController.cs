using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Authorization;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Models.Requests;
using SASI.Servicios;
using System.Security.Claims;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class SoporteController : Controller
    {
        private readonly ISoporteServicio _soporteServicio;
        private readonly ISistemaServicio _sistemaServicio;
        private readonly IRolServicio _rolServicio;

        public SoporteController(
            ISoporteServicio soporteServicio,
            ISistemaServicio sistemaServicio,
            IRolServicio rolServicio)
        {
            _soporteServicio = soporteServicio;
            _sistemaServicio = sistemaServicio;
            _rolServicio = rolServicio;
        }

        private string UsuarioActual => User.Identity?.Name ?? "";

        // ===================== INCIDENCIAS =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Incidencias(int? sistemaId)
        {
            var sistemas = await _sistemaServicio.ListarAsync();
            ViewBag.Sistemas = sistemas.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.IdSistema.ToString(),
                Text = $"{s.Codigo} - {s.Nombre}"
            }).ToList();

            ViewBag.SistemaFiltro = sistemaId;
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ListarIncidencias(int? sistemaId)
        {
            var incidencias = await _soporteServicio.ListarIncidenciasAsync(sistemaId);
            return Json(incidencias);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarIncidencia([FromForm] IncidenciaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var dto = new IncidenciaDto
            {
                IdIncidencia = request.IdIncidencia,
                SistemaId = request.SistemaId,
                Titulo = request.Titulo,
                Descripcion = request.Descripcion ?? "",
                Prioridad = request.Prioridad,
                Estado = request.Estado,
                Responsable = request.Responsable
            };

            var resultado = request.IdIncidencia > 0
                ? await _soporteServicio.EditarIncidenciaAsync(dto, UsuarioActual)
                : await _soporteServicio.RegistrarIncidenciaAsync(dto, UsuarioActual);

            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoIncidencia([FromBody] CambiarEstadoIncidenciaRequest request)
        {
            var resultado = await _soporteServicio.CambiarEstadoIncidenciaAsync(request.Id, request.Estado, UsuarioActual);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarIncidencia(int id)
        {
            var resultado = await _soporteServicio.EliminarIncidenciaAsync(id);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        // ===================== SOLICITUDES DE ACCESO =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Solicitudes()
        {
            var sistemas = await _sistemaServicio.ListarAsync();

            var pendientes = await _soporteServicio.ListarSolicitudesAsync(EstadoSolicitudAcceso.Pendiente);
            var todas = await _soporteServicio.ListarSolicitudesAsync();

            var vm = new Models.SolicitudesAccesoViewModel
            {
                Pendientes = pendientes.Where(s => s.Estado == EstadoSolicitudAcceso.Pendiente).ToList(),
                Respondidas = todas.Where(s => s.Estado != EstadoSolicitudAcceso.Pendiente).ToList(),
                Sistemas = sistemas.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = s.IdSistema.ToString(),
                    Text = $"{s.Codigo} - {s.Nombre}"
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ObtenerRolesPorSistema(int sistemaId)
        {
            var roles = await _rolServicio.ObtenerPorSistemaIdAsync(sistemaId);
            return Json(roles.Select(r => new { r.IdRol, r.Nombre }));
        }

        [HttpPost]
        public async Task<IActionResult> CrearSolicitud([FromForm] SolicitudAccesoRequest request)
        {
            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(usuarioIdStr, out var usuarioId))
                return BadRequest(new { success = false, mensaje = "No se pudo identificar al usuario." });

            var resultado = await _soporteServicio.CrearSolicitudAsync(request.SistemaId, request.RolId, request.Justificacion ?? "", usuarioId);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> AprobarSolicitud([FromBody] ResponderSolicitudRequest request)
        {
            var resultado = await _soporteServicio.AprobarSolicitudAsync(request.IdSolicitud, UsuarioActual, request.Comentario);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> RechazarSolicitud([FromBody] ResponderSolicitudRequest request)
        {
            var resultado = await _soporteServicio.RechazarSolicitudAsync(request.IdSolicitud, UsuarioActual, request.Comentario);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }

        // ===================== MONITOREO / ESTADO OPERATIVO =====================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Monitoreo()
        {
            var sistemas = await _sistemaServicio.ListarAsync();
            var sistemasVm = sistemas.Select(s => new
            {
                s.IdSistema,
                s.Codigo,
                s.Nombre,
                EstadoOperativoActual = s.EstadoOperativoActual.ToString()
            }).ToList();

            ViewBag.Sistemas = sistemasVm;
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> HistorialEstado(int sistemaId)
        {
            var historial = await _soporteServicio.ObtenerHistorialEstadoAsync(sistemaId);
            return Json(historial);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoOperativo([FromForm] EstadoOperativoRequest request)
        {
            var resultado = await _soporteServicio.CambiarEstadoOperativoAsync(request.SistemaId, request.Estado, request.Observacion, UsuarioActual);
            return Json(new { success = resultado.Exito, mensaje = resultado.Mensaje });
        }
    }
}
