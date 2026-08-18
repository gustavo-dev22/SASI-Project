using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASI.Aplicacion.Servicios;
using SASI.Infraestructura.Identity;
using SASI.Models;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize(Policy = "AccesoModulo")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly ISistemaServicio _sistemaServicio;
        private readonly IRolServicio _rolServicio;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsuarioController(
            IUsuarioSistemaServicio usuarioSistemaServicio,
            ISistemaServicio sistemaServicio,
            IRolServicio rolServicio,
            UserManager<ApplicationUser> userManager)
        {
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _sistemaServicio = sistemaServicio;
            _rolServicio = rolServicio;
            _userManager = userManager;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(int sistemaId, int page = 1)
        {
            const int pageSize = 10;

            var sistema = await _sistemaServicio.ObtenerPorIdAsync(sistemaId);
            if (sistema == null) return NotFound();

            var usuarios = await _usuarioSistemaServicio.ObtenerUsuariosPorSistemaAsync(sistemaId);
            var roles = await _rolServicio.ObtenerRolesComoSelectListAsync(sistema.IdSistema);

            var pagedUsuarios = usuarios
                .OrderBy(u => u.NombreCompleto)
                .ToPagedList(page < 1 ? 1 : page, pageSize);

            var vm = new UsuarioSistemaViewModel
            {
                SistemaId = sistema.IdSistema,
                CodigoSistema = sistema.Codigo,
                NombreSistema = sistema.Nombre,
                UsuariosAsignados = pagedUsuarios,
                RolesDisponibles = roles
            };

            return View(vm);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ObtenerUsuariosDisponibles(int sistemaId)
        {
            var asignados = await _usuarioSistemaServicio.ObtenerUsuariosPorSistemaAsync(sistemaId);
            var idsAsignados = asignados.Select(a => a.UsuarioId).ToHashSet();

            var disponibles = await _userManager.Users
                .Where(u => u.Activo && !idsAsignados.Contains(u.Id))
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new { id = u.Id, nombreCompleto = u.NombreCompleto, email = u.Email })
                .ToListAsync();

            return Json(disponibles);
        }

        [HttpPost]
        public async Task<IActionResult> AsignarUsuario(int sistemaId, Guid usuarioId, int rolId, bool esPrincipal)
        {
            if (sistemaId <= 0 || usuarioId == Guid.Empty || rolId <= 0)
                return BadRequest(new { success = false, message = "Datos inválidos." });

            var resultado = await _usuarioSistemaServicio.AsignarUsuarioASistemaAsync(
                usuarioId.ToString(), sistemaId, rolId, esPrincipal);

            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> QuitarUsuarioDeSistema(Guid usuarioId, int sistemaId, int rolId)
        {
            var resultado = await _usuarioSistemaServicio.QuitarRolUsuarioDeSistemaAsync(usuarioId, sistemaId, rolId);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }
    }
}
