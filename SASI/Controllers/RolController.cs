using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Authorization;
using SASI.Dominio.Modelo;
using SASI.Models;
using SASI.Models.Requests;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class RolController : Controller
    {
        private readonly IRolServicio _rolServicio;
        private readonly ISistemaServicio _sistemaServicio;

        public RolController(IRolServicio rolServicio, ISistemaServicio sistemaServicio)
        {
            _rolServicio = rolServicio;
            _sistemaServicio = sistemaServicio;
        }

        public async Task<IActionResult> Index(int sistemaId, int? page)
        {
            int pageSize = 5;
            int pageNumber = page ?? 1;

            var roles = await _rolServicio.ObtenerPorSistemaIdAsync(sistemaId);
            var sistema = await _sistemaServicio.ObtenerPorIdAsync(sistemaId);

            if (sistema == null)
                return NotFound();

            ViewBag.SistemaId = sistemaId;
            ViewBag.NombreSistema = sistema.Nombre;

            var pagedRoles = roles.ToPagedList(pageNumber, pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(pagedRoles);
        }

        [HttpGet]
        public IActionResult Crear(int sistemaId)
        {
            var rol = new Rol { IdSistema = sistemaId };
            return PartialView("_CrearRolPartial", rol);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Rol rol)
        {
            if (!ModelState.IsValid)
                return PartialView("_CrearRolPartial", rol);

            await _rolServicio.CrearAsync(rol);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado([FromBody] EliminarRolRequest request)
        {
            var resultado = await _rolServicio.CambiarEstadoAsync(request.Id);
            return Json(new { success = resultado.Exito, estado = resultado.Estado });
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var rol = await _rolServicio.ObtenerPorIdAsync(id);
            if (rol == null)
                return NotFound();

            return PartialView("_CrearRolPartial", rol);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Rol rol)
        {
            if (!ModelState.IsValid)
                return PartialView("_CrearRolPartial", rol);

            await _rolServicio.EditarAsync(rol);
            return Json(new { success = true });
        }

        public async Task<IActionResult> AsignarObjetos(int idRol)
        {
            var rol = await _rolServicio.ObtenerPorIdAsync(idRol);
            if (rol == null)
                return NotFound();

            var objetos = await _rolServicio.ObtenerObjetosPorSistemaAsync(rol.IdSistema);
            var asignados = await _rolServicio.ObtenerIdsObjetosPorRolAsync(idRol);

            var viewModel = new AsignarObjetosViewModel
            {
                IdRol = idRol,
                NombreRol = rol.Nombre,
                Objetos = objetos,
                IdsAsignados = asignados
            };

            ViewBag.SistemaId = rol.IdSistema;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarAsignacionObjetos(AsignarObjetosViewModel model)
        {
            await _rolServicio.GuardarAsignacionObjetosAsync(model.IdRol, model.IdsAsignados);

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("AsignarObjetos", "Rol", new { idRol = model.IdRol })
            });
        }

        [HttpGet]
        public async Task<IActionResult> ValidarObjetosPorSistema(int idRol)
        {
            var idSistema = _rolServicio.ObtenerIdSistemaPorRol(idRol);

            var hayObjetos = await _rolServicio.ExistenObjetosParaSistemaAsync(idSistema);

            return Json(new { existe = hayObjetos });
        }
    }
}
