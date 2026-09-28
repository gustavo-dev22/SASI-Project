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
        private readonly IPermisoServicio _permisoServicio;

        public RolController(IRolServicio rolServicio, ISistemaServicio sistemaServicio, IPermisoServicio permisoServicio)
        {
            _rolServicio = rolServicio;
            _sistemaServicio = sistemaServicio;
            _permisoServicio = permisoServicio;
        }

        [PermisoAccion("Rol", AccionesSistema.Listar)]
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
        [PermisoAccion("Rol", AccionesSistema.Crear)]
        public IActionResult Crear(int sistemaId)
        {
            var rol = new Rol { IdSistema = sistemaId };
            return PartialView("_CrearRolPartial", rol);
        }

        [HttpPost]
        [PermisoAccion("Rol", AccionesSistema.Crear)]
        public async Task<IActionResult> Crear(Rol rol)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    mensaje = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                });

            await _rolServicio.CrearAsync(rol);
            return Json(new { success = true });
        }

        [HttpPost]
        [PermisoAccion("Rol", AccionesSistema.Bloquear)]
        public async Task<IActionResult> CambiarEstado([FromBody] EliminarRolRequest request)
        {
            var resultado = await _rolServicio.CambiarEstadoAsync(request.Id);
            return Json(new { success = resultado.Exito, estado = resultado.Estado });
        }

        [HttpGet]
        [PermisoAccion("Rol", AccionesSistema.Editar)]
        public async Task<IActionResult> Editar(int id)
        {
            var rol = await _rolServicio.ObtenerPorIdAsync(id);
            if (rol == null)
                return NotFound();

            return PartialView("_CrearRolPartial", rol);
        }

        [HttpPost]
        [PermisoAccion("Rol", AccionesSistema.Editar)]
        public async Task<IActionResult> Editar(Rol rol)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    mensaje = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                });

            await _rolServicio.EditarAsync(rol);
            return Json(new { success = true });
        }

        [PermisoAccion("Rol", AccionesSistema.Editar)]
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
        [PermisoAccion("Rol", AccionesSistema.Editar)]
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
        [PermisoAccion("Rol", AccionesSistema.Editar)]
        public async Task<IActionResult> AsignarAcciones(int idRol)
        {
            var rol = await _rolServicio.ObtenerPorIdAsync(idRol);
            if (rol == null)
                return NotFound();

            var objetos = (await _rolServicio.ObtenerObjetosPorSistemaAsync(rol.IdSistema))
                .Where(o => o.Activo
                            && !string.IsNullOrWhiteSpace(o.Url)
                            && o.Url!.Trim() != "#")
                .ToList();
            var acciones = await _permisoServicio.ObtenerCatalogoAccionesAsync();
            var asignaciones = await _rolServicio.ObtenerAsignacionesAccionesPorRolAsync(idRol);
            var sistema = await _sistemaServicio.ObtenerPorIdAsync(rol.IdSistema);

            var viewModel = new PermisoRolViewModel
            {
                IdRol = idRol,
                NombreRol = rol.Nombre,
                IdSistema = rol.IdSistema,
                NombreSistema = sistema?.Nombre ?? string.Empty,
                Objetos = objetos,
                Acciones = acciones,
                PermisosSeleccionados = asignaciones.Select(a => $"{a.IdObjeto}:{a.IdAccion}").ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [PermisoAccion("Rol", AccionesSistema.Editar)]
        public async Task<IActionResult> GuardarAsignacionAcciones([FromBody] GuardarPermisosRequest request)
        {
            if (request == null || request.IdRol <= 0)
                return Json(new { success = false, mensaje = "Datos inválidos." });

            var catalogo = await _permisoServicio.ObtenerCatalogoAccionesAsync();
            var idListar = catalogo.FirstOrDefault(a => a.Codigo == AccionesSistema.Listar)?.IdAccion;

            var asignaciones = (request.Permisos ?? new List<PermisoItemRequest>())
                .GroupBy(p => new { p.IdObjeto, p.IdAccion })
                .Select(g => (g.Key.IdObjeto, g.Key.IdAccion))
                .ToList();

            // Regla base: si un objeto tiene acciones concedidas, garantizar LISTAR.
            if (idListar.HasValue)
            {
                var objetosConAccion = asignaciones.Select(a => a.IdObjeto).Distinct().ToList();
                foreach (var idObjeto in objetosConAccion)
                {
                    if (!asignaciones.Any(a => a.IdObjeto == idObjeto && a.IdAccion == idListar.Value))
                        asignaciones.Add((idObjeto, idListar.Value));
                }
            }

            await _rolServicio.GuardarAsignacionAccionesAsync(request.IdRol, asignaciones);

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("AsignarAcciones", "Rol", new { idRol = request.IdRol })
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
