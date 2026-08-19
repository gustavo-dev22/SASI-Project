using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Authorization;
using SASI.Servicios;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class ReporteController : Controller
    {
        private readonly IReporteServicio _reporteServicio;

        public ReporteController(IReporteServicio reporteServicio)
        {
            _reporteServicio = reporteServicio;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            var resumen = await _reporteServicio.ResumenPorSistemaAsync();
            var sistemasSinRoles = await _reporteServicio.SistemasSinRolesAsync();
            var rolesSinObjetos = await _reporteServicio.RolesSinObjetosAsync();
            var oficinasSinUsuarios = await _reporteServicio.OficinasSinUsuariosAsync();

            ViewBag.Resumen = resumen;
            ViewBag.SistemasSinRoles = sistemasSinRoles;
            ViewBag.RolesSinObjetos = rolesSinObjetos;
            ViewBag.OficinasSinUsuarios = oficinasSinUsuarios;

            return View();
        }
    }
}
