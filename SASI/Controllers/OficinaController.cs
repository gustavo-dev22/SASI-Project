using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Repositories;
using SASI.Models;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize]
    public class OficinaController : Controller
    {
        private readonly IOficinaRepository _oficinaRepository;

        public OficinaController(IOficinaRepository oficinaRepository)
        {
            _oficinaRepository = oficinaRepository;
        }

        public async Task<IActionResult> Index(int? page)
        {
            var oficinas = await _oficinaRepository.ListarAsync();

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

        public IActionResult Crear() => View();

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] OficinaViewModel modelo)
        {
            ModelState.Clear();

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

                await _oficinaRepository.CrearAsync(oficina);

                return Ok(new { success = true, mensaje = "Oficina creada correctamente", idOficina = modelo.IdOficina });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al crear la oficina." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Editar([FromBody] Oficina oficina)
        {
            try
            {
                await _oficinaRepository.Actualizar(oficina);
                return Ok(new { success = true, mensaje = "Oficina editada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al editar la oficina." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var oficinas = await _oficinaRepository.ListarActivasAsync();
            return Ok(oficinas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstado(int id)
        {
            var resultado = await _oficinaRepository.ActualizarEstadoAsync(id);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var oficina = await _oficinaRepository.ObtenerPorId(id);
            if (oficina == null)
                return NotFound();

            return Json(oficina);
        }
    }
}
