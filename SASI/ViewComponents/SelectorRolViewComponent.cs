using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SASI.Configuration;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using System.Security.Claims;

namespace SASI.ViewComponents
{
    public class SelectorRolViewComponent : ViewComponent
    {
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly int _sistemaId;

        public SelectorRolViewComponent(IUsuarioSistemaRepository usuarioSistemaRepository, IHttpContextAccessor httpContextAccessor, IOptions<ConfiguracionSistemaSASI> config)
        {
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _httpContextAccessor = httpContextAccessor;
            _sistemaId = config.Value.Id;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = Guid.Parse(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

            var roles = await _usuarioSistemaRepository.ObtenerRolesDelUsuarioEnSistema(userId, _sistemaId);

            // Evitar error si no hay roles
            if (roles == null)
                roles = new List<Rol>();

            var selectedRol = HttpContext.Session.GetInt32("RolSeleccionado");
            ViewBag.SelectedRolId = selectedRol;

            return View(roles);
        }
    }
}
