using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SASI.Configuration;
using SistemaConvocatorias.Infraestructura.Datos;
using System.Security.Claims;

namespace SASI.Authorization
{
    public class AccesoModuloRequirement : IAuthorizationRequirement
    {
    }

    public class AccesoModuloHandler : AuthorizationHandler<AccesoModuloRequirement>
    {
        private const string RolAdministrador = "Administrador";
        private const string RolAdministradorSeguridad = "Administrador de Seguridad";

        private readonly SasiDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly int _sistemaId;

        public AccesoModuloHandler(
            SasiDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IOptions<ConfiguracionSistemaSASI> config)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _sistemaId = config.Value.Id;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccesoModuloRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return;

            // Los administradores tienen acceso a todos los módulos.
            if (context.User.IsInRole(RolAdministrador) || context.User.IsInRole(RolAdministradorSeguridad))
            {
                context.Succeed(requirement);
                return;
            }

            var controller = httpContext.GetRouteValue("controller")?.ToString();
            var action = httpContext.GetRouteValue("action")?.ToString();
            if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action)) return;

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var usuarioGuid)) return;

            int? rolSeleccionado = null;
            if (httpContext.Session.IsAvailable)
            {
                rolSeleccionado = httpContext.Session.GetInt32("RolSeleccionado");
            }

            // Consulta única: si hay rol seleccionado, se valida sobre ese rol;
            // si no, se valida sobre cualquier rol activo del usuario en el sistema.
            var tieneAcceso = rolSeleccionado.HasValue
                ? await (
                        from ro in _context.RolObjetos
                        join obj in _context.Objetos on ro.IdObjeto equals obj.IdObjeto
                        where ro.IdRol == rolSeleccionado.Value && ro.Activo && obj.Activo
                              && obj.Url != null && EF.Functions.Like(obj.Url, controller + "%")
                        select 1)
                    .AnyAsync()
                : await (
                        from ro in _context.RolObjetos
                        join obj in _context.Objetos on ro.IdObjeto equals obj.IdObjeto
                        join us in _context.UsuarioSistemas on ro.IdRol equals us.RolId
                        where us.UsuarioId == usuarioGuid && us.SistemaId == _sistemaId && us.Activo
                              && ro.Activo && obj.Activo
                              && obj.Url != null && EF.Functions.Like(obj.Url, controller + "%")
                        select 1)
                    .AnyAsync();

            if (tieneAcceso) context.Succeed(requirement);
        }
    }
}
