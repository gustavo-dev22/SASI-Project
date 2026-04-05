using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Repositories;
using SASI.Dominio.DTO;
using SASI.Models;

namespace SASI.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;
        private readonly ISistemaRepository _sistemaRepository;
        private readonly IRolRepository _rolRepository;

        public UsuarioController(IUsuarioSistemaRepository usuarioSistemaRepository, ISistemaRepository sistemaRepository, IRolRepository rolRepository)
        {
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _sistemaRepository = sistemaRepository;
            _rolRepository = rolRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int sistemaId)
        {
            var sistema = await _sistemaRepository.ObtenerPorId(sistemaId);
            if (sistema == null) return NotFound();

            var usuarios = await _usuarioSistemaRepository.ObtenerUsuariosPorSistemaAsync(sistemaId);
            var roles = await _rolRepository.ObtenerRolesComoSelectListAsync(sistema.IdSistema);

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
