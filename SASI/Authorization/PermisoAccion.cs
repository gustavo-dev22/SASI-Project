using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SASI.Aplicacion.Servicios;
using SASI.Configuration;
using System.Security.Claims;

namespace SASI.Authorization
{
    // Verifica que el rol activo del usuario tenga la acción indicada sobre el módulo.
    // - Administradores de SASI tienen bypass.
    // - Compatibilidad: si el rol aún no tiene acciones configuradas para el módulo,
    //   se permite (el enforcement inicia cuando el administrador configura acciones).
    // Uso: [PermisoAccion("Objeto", AccionesSistema.Crear)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PermisoAccionAttribute : Attribute, IAsyncActionFilter
    {
        private const string RolAdministrador = "Administrador";
        private const string RolAdministradorSeguridad = "Administrador de Seguridad";

        public string Modulo { get; }
        public string Accion { get; }

        public PermisoAccionAttribute(string modulo, string accion)
        {
            Modulo = modulo;
            Accion = accion;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var user = http.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            if (user.IsInRole(RolAdministrador) || user.IsInRole(RolAdministradorSeguridad))
            {
                await next();
                return;
            }

            var services = http.RequestServices;
            var permisoServicio = services.GetRequiredService<IPermisoServicio>();
            var usuarioSistemaServicio = services.GetRequiredService<IUsuarioSistemaServicio>();
            var sistemaId = services.GetRequiredService<IOptions<ConfiguracionSistemaSASI>>().Value.Id;

            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            var idObjeto = await permisoServicio.ObtenerIdObjetoPorUrlAsync(sistemaId, Modulo);
            if (!idObjeto.HasValue)
            {
                // El módulo no está mapeado como objeto: no se puede evaluar, no se bloquea.
                await next();
                return;
            }

            int? rolSeleccionado = http.Session.IsAvailable
                ? http.Session.GetInt32("RolSeleccionado")
                : null;

            bool permitido;
            if (rolSeleccionado.HasValue)
            {
                permitido = await EvaluarRolAsync(permisoServicio, sistemaId, rolSeleccionado.Value, idObjeto.Value);
            }
            else
            {
                permitido = false;
                var roles = await usuarioSistemaServicio.ObtenerRolesDelUsuarioEnSistemaAsync(userId, sistemaId);
                foreach (var rol in roles)
                {
                    if (await EvaluarRolAsync(permisoServicio, sistemaId, rol.IdRol, idObjeto.Value))
                    {
                        permitido = true;
                        break;
                    }
                }
            }

            if (!permitido)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            await next();
        }

        private async Task<bool> EvaluarRolAsync(IPermisoServicio permisoServicio, int sistemaId, int idRol, int idObjeto)
        {
            var tieneConfiguradas = await permisoServicio.TieneAlgunaAccionConfiguradaAsync(idRol, idObjeto);
            if (!tieneConfiguradas)
                return true;

            return await permisoServicio.TienePermisoAsync(sistemaId, idRol, idObjeto, Accion);
        }
    }
}
