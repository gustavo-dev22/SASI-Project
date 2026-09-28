using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Infraestructura.Identity;

namespace SASI.Controllers.API
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/permisos")]
    public class PermisosApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;
        private readonly IObjetoServicio _objetoServicio;
        private readonly IPermisoServicio _permisoServicio;

        public PermisosApiController(
            UserManager<ApplicationUser> userManager,
            IUsuarioSistemaServicio usuarioSistemaServicio,
            IObjetoServicio objetoServicio,
            IPermisoServicio permisoServicio)
        {
            _userManager = userManager;
            _usuarioSistemaServicio = usuarioSistemaServicio;
            _objetoServicio = objetoServicio;
            _permisoServicio = permisoServicio;
        }

        // GET /api/permisos/{sistemaId}/{usuario}
        [HttpGet("{sistemaId:int}/{usuario}")]
        public async Task<IActionResult> ObtenerPermisos(int sistemaId, string usuario)
        {
            var user = await _userManager.FindByNameAsync(usuario)
                       ?? await _userManager.FindByEmailAsync(usuario);

            if (user == null)
                return NotFound(new { exito = false, mensaje = "Usuario no encontrado." });

            var objetosSistema = (await _objetoServicio.ObtenerPorSistemaAsync(sistemaId))
                .ToDictionary(o => o.IdObjeto, o => o.Url ?? string.Empty);

            var rolesUsuario = await _usuarioSistemaServicio.ObtenerRolesDelUsuarioEnSistemaAsync(user.Id, sistemaId);

            var roles = new List<object>();
            foreach (var rol in rolesUsuario)
            {
                var permisos = await _permisoServicio.ObtenerPermisosPorRolSistemaAsync(rol.IdRol, sistemaId);

                roles.Add(new
                {
                    idRol = rol.IdRol,
                    nombreRol = rol.Nombre,
                    objetos = permisos.Select(p => new
                    {
                        idObjeto = p.Key,
                        url = objetosSistema.GetValueOrDefault(p.Key, string.Empty),
                        acciones = p.Value
                    }).ToList()
                });
            }

            return Ok(new
            {
                exito = true,
                datos = new
                {
                    sistemaId,
                    usuario = user.UserName,
                    roles
                }
            });
        }
    }
}
