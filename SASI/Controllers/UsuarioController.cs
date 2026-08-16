using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Models;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly ISistemaServicio _sistemaServicio;
        private readonly IRolServicio _rolServicio;

        public UsuarioController(IUsuarioSistemaServicio usuarioSistemaServicio, ISistemaServicio sistemaServicio, IRolServicio rolServicio)
        {
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _sistemaServicio = sistemaServicio;
            _rolServicio = rolServicio;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int sistemaId)
        {
            var sistema = await _sistemaServicio.ObtenerPorIdAsync(sistemaId);
            if (sistema == null) return NotFound();

            var usuarios = await _usuarioSistemaServicio.ObtenerUsuariosPorSistemaAsync(sistemaId);
            var roles = await _rolServicio.ObtenerRolesComoSelectListAsync(sistema.IdSistema);

            var vm = new UsuarioSistemaViewModel
            {
                SistemaId = sistema.IdSistema,
                CodigoSistema = sistema.Codigo,
                NombreSistema = sistema.Nombre,
                UsuariosAsignados = usuarios,
                RolesDisponibles = roles
            };

            return View(vm);
        }
    }
}
