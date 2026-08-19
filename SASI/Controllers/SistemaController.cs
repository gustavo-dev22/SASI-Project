using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Authorization;
using SASI.Dominio.Modelo;
using SASI.Helpers;
using SASI.Models;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class SistemaController : Controller
    {
        private readonly ISistemaServicio _sistemaServicio;

        public SistemaController(ISistemaServicio sistemaServicio)
        {
            _sistemaServicio = sistemaServicio;
        }

        public async Task<IActionResult> Index(int? page)
        {
            var sistemas = await _sistemaServicio.ListarAsync();

            // Proyección al ViewModel
            var sistemasViewModel = sistemas.Select(s => new SistemaViewModel
            {
                IdSistema = s.IdSistema,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                FechaRegistro = s.FechaRegistro,
                Estado = s.Activo,
                CantidadRoles = s.Roles?.Count() ?? 0
            }).ToList();

            // Contar los activos
            ViewBag.Total = sistemasViewModel.Count();
            ViewBag.TotalActivos = sistemasViewModel.Count(s => s.Estado);
            ViewBag.TotalInactivos = sistemasViewModel.Count(s => !s.Estado);

            // Paginación
            int pageSize = 10;
            int pageNumber = page ?? 1;
            var pagedSistemas = sistemasViewModel
                .OrderByDescending(s => s.FechaRegistro)
                .ToPagedList(pageNumber, pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(pagedSistemas);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProximoCodigo()
        {
            var codigo = await _sistemaServicio.ObtenerProximoCodigoAsync();
            return Ok(new { codigo });
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Sistema modelo)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos" });

            try
            {
                var resultado = await _sistemaServicio.CrearAsync(modelo);
                return Ok(new { success = true, mensaje = resultado.Mensaje, codigo = resultado.Codigo });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al crear el sistema." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstado(int id)
        {
            var resultado = await _sistemaServicio.ActualizarEstadoAsync(id);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpPost]
        public async Task<IActionResult> Editar([FromBody] Sistema sistema)
        {
            try
            {
                var resultado = await _sistemaServicio.ActualizarAsync(sistema);
                return Ok(new { success = true, mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al editar el sistema." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var sistema = await _sistemaServicio.ObtenerPorIdAsync(id);
            if (sistema == null)
                return NotFound();

            return Json(sistema);
        }

        public async Task<IActionResult> UsuariosPorSistema(int sistemaId, int page = 1)
        {
            var pageSize = 5;

            var usuarios = await _sistemaServicio.ObtenerUsuariosConRolesPorSistemaAsync(sistemaId);

            var sistema = await _sistemaServicio.ObtenerPorIdAsync(sistemaId);

            ViewBag.SistemaId = sistemaId;
            ViewBag.NombreSistema = sistema?.Nombre ?? "";

            var pagedUsuarios = usuarios.ToPagedList(page, pageSize);

            var html = await this.RenderViewAsync("_UsuariosPorSistemaPartial", pagedUsuarios, true);

            return Json(new
            {
                html,
                nombreSistema = sistema?.Nombre ?? ""
            });
        }
    }
}
