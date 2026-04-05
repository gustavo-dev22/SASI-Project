using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Repositories;
using SASI.Models;
using SASI.Models.Requests;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize]
    public class RolController : Controller
    {
        private readonly IRolRepository _rolRepository;
        private readonly ISistemaRepository _sistemaRepository;
        private readonly IObjetoRepository _objetoRepository;
        private readonly IRolObjetoRepository _rolObjetoRepository;

        public RolController(IRolRepository rolRepository, ISistemaRepository sistemaRepository, IObjetoRepository objetoRepository, IRolObjetoRepository rolObjetoRepository)
        {
            _rolRepository = rolRepository;
            _sistemaRepository = sistemaRepository;
            _objetoRepository = objetoRepository;
            _rolObjetoRepository = rolObjetoRepository;
        }

        public async Task<IActionResult> Index(int sistemaId, int? page)
        {
            int pageSize = 5;
            int pageNumber = page ?? 1;

            var roles = await _rolRepository.ObtenerPorSistemaId(sistemaId);
            var sistema = await _sistemaRepository.ObtenerPorId(sistemaId);

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
            ModelState.Clear();

            if (!ModelState.IsValid)
                return PartialView("_CrearRolPartial", rol);

            await _rolRepository.Crear(rol);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado([FromBody] EliminarRolRequest request)
        {
            var rol = await _rolRepository.ObtenerPorId(request.Id);
            if (rol == null)
                return Json(new { success = false });

            rol.Activo = !rol.Activo;
            await _rolRepository.Editar(rol);

            return Json(new { success = true, estado = rol.Activo });
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var rol = await _rolRepository.ObtenerPorId(id);
            if (rol == null)
                return NotFound();

            return PartialView("_CrearRolPartial", rol);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Rol rol)
        {
            ModelState.Clear();

            if (!ModelState.IsValid)
                return PartialView("_CrearRolPartial", rol);

            await _rolRepository.Editar(rol);
            return Json(new { success = true });
        }

        public async Task<IActionResult> AsignarObjetos(int idRol)
        {
            var rol = await _rolRepository.ObtenerPorId(idRol);
            if (rol == null)
                return NotFound();

            var objetos = await _objetoRepository.ObtenerPorSistemaAsync(rol.IdSistema);
            var asignados = await _rolObjetoRepository.ObtenerIdsObjetosPorRolAsync(idRol);

            var viewModel = new AsignarObjetosViewModel
            {
                IdRol = idRol,
                NombreRol = rol.Nombre,
                Objetos = (List<Objeto>)objetos,
                IdsAsignados = asignados
            };

            ViewBag.SistemaId = rol.IdSistema;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarAsignacionObjetos(AsignarObjetosViewModel model)
        {
            await _rolObjetoRepository.ActualizarAsignacionesAsync(model.IdRol, model.IdsAsignados);

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("AsignarObjetos", "Rol", new { idRol = model.IdRol })
            });
        }

        [HttpGet]
        public async Task<IActionResult> ValidarObjetosPorSistema(int idRol)
        {
            // Suponiendo que puedes obtener el IdSistema asociado al rol
            var idSistema = _rolRepository.ObtenerIdSistemaPorRol(idRol);

            var hayObjetos = await _objetoRepository.ExistenObjetosParaSistema(idSistema); // Devuelve bool

            return Json(new { existe = hayObjetos });
        }
    }
}
