using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Dominio.Modelo;
using SASI.Models;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize]
    public class OficinaController : Controller
    {
        private readonly IOficinaServicio _oficinaServicio;

        public OficinaController(IOficinaServicio oficinaServicio)
        {
            _oficinaServicio = oficinaServicio;
        }

        [Authorize(Policy = "AccesoModulo")]
        public async Task<IActionResult> Index(int? page)
        {
            var oficinas = await _oficinaServicio.ListarAsync();

            var oficinasViewModel = oficinas.Select(s => new OficinaViewModel
            {
                IdOficina = s.IdOficina,
                Nombre = s.Nombre,
                Sigla = s.Sigla,
                IdOficinaPadre = s.IdOficinaPadre,
                Activo = s.Activo
            }).ToList();

            ViewBag.Total = oficinasViewModel.Count();
            ViewBag.TotalActivos = oficinasViewModel.Count(s => s.Activo);
            ViewBag.TotalInactivos = oficinasViewModel.Count(s => !s.Activo);

            int pageSize = 10;
            int pageNumber = page ?? 1;
            var pagedOficinas = oficinasViewModel
                .OrderByDescending(s => s.IdOficina)
                .ToPagedList(pageNumber, pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(pagedOficinas);
        }

        [Authorize(Policy = "AccesoModulo")]
        public IActionResult Crear() => View();

        [HttpPost]
        [Authorize(Policy = "AccesoModulo")]
        public async Task<IActionResult> Crear([FromBody] OficinaViewModel modelo)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos" });

            try
            {
                var oficina = new Oficina
                {
                    Nombre = modelo.Nombre.Trim(),
                    Sigla = modelo.Sigla.Trim(),
                    Activo = modelo.Activo,
                    IdOficinaPadre = modelo.TieneOficinaPadre ? null : modelo.IdOficinaPadre
                };

                await _oficinaServicio.CrearAsync(oficina);

                return Ok(new { success = true, mensaje = "Oficina creada correctamente", idOficina = modelo.IdOficina });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al crear la oficina." });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccesoModulo")]
        public async Task<IActionResult> Editar([FromBody] Oficina oficina)
        {
            try
            {
                await _oficinaServicio.ActualizarAsync(oficina);
                return Ok(new { success = true, mensaje = "Oficina editada correctamente" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al editar la oficina." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var oficinas = await _oficinaServicio.ListarActivasAsync();
            return Ok(oficinas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AccesoModulo")]
        public async Task<IActionResult> ActualizarEstado(int id)
        {
            var resultado = await _oficinaServicio.ActualizarEstadoAsync(id);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var oficina = await _oficinaServicio.ObtenerPorIdAsync(id);
            if (oficina == null)
                return NotFound();

            return Json(oficina);
        }
    }
}
