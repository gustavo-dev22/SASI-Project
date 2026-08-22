using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;

namespace SASI.Controllers.API
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/objetos")]
    public class ObjetosApiController : Controller
    {
        private readonly IObjetoServicio _objetoServicio;
        private readonly IRolServicio _rolServicio;

        public ObjetosApiController(IObjetoServicio objetoServicio, IRolServicio rolServicio)
        {
            _objetoServicio = objetoServicio;
            _rolServicio = rolServicio;
        }

        [HttpGet("PorSistema/{idSistema}")]
        public async Task<IActionResult> ObtenerObjetosPorSistema(int idSistema)
        {
            try
            {
                var objetos = await _objetoServicio.ObtenerPorSistemaAsync(idSistema);

                if (objetos == null || !objetos.Any())
                {
                    return Ok(new
                    {
                        exito = true,
                        mensaje = "No existen objetos registrados para este sistema.",
                        datos = new List<object>()
                    });
                }

                var resultado = objetos.Select(o => new
                {
                    o.IdObjeto,
                    o.Nombre,
                    o.IdPadre,
                    o.Url,
                    o.Tipo,
                    o.Orden
                });

                return Ok(new
                {
                    exito = true,
                    mensaje = "Objetos obtenidos correctamente.",
                    datos = resultado
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al consultar los objetos del sistema."
                });
            }
        }

        [HttpGet("RolesPorSistema/{idSistema}")]
        public async Task<IActionResult> ObtenerRolesPorSistema(int idSistema)
        {
            try
            {
                var roles = await _rolServicio.ObtenerPorSistemaIdAsync(idSistema);

                if (roles == null || !roles.Any())
                {
                    return Ok(new
                    {
                        exito = true,
                        mensaje = "No existen roles registrados para este sistema.",
                        datos = new List<object>()
                    });
                }

                var resultado = roles.Select(r => new
                {
                    r.IdRol,
                    r.Nombre,
                    r.Activo
                });

                return Ok(new
                {
                    exito = true,
                    mensaje = "Roles obtenidos correctamente.",
                    datos = resultado
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al consultar los roles del sistema."
                });
            }
        }

        [HttpGet("PorSistemaYRolNombre")]
        public async Task<IActionResult> ObtenerObjetosPorSistemaYRolNombre([FromQuery] int sistemaId, [FromQuery] string rolNombre)
        {
            try
            {
                var objetos = await _objetoServicio.ObtenerPorSistemaYRolNombreAsync(sistemaId, rolNombre);

                if (objetos == null || !objetos.Any())
                {
                    return Ok(new
                    {
                        exito = true,
                        mensaje = "No existen objetos asignados para este rol.",
                        datos = new List<object>()
                    });
                }

                var resultado = objetos.Select(o => new
                {
                    idObjeto = o.IdObjeto,
                    nombre = o.Nombre,
                    tipo = o.Tipo,
                    url = o.Url,
                    titulo = o.Titulo ?? o.Nombre,
                    icono = o.Icono ?? "fa-solid fa-circle",
                    activo = o.Activo,
                    orden = o.Orden,
                    idPadre = o.IdPadre
                });

                return Ok(new
                {
                    exito = true,
                    mensaje = "Menús obtenidos correctamente.",
                    datos = resultado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al consultar los menús del rol: " + ex.Message
                });
            }
        }
    }
}
