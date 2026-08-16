using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;

namespace SASI.Controllers.API
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/sistemas")]
    public class SistemasApiController : Controller
    {
        private readonly ISistemaServicio _sistemaServicio;
        private readonly IUsuarioSistemaServicio _usuarioSistemaServicio;

        public SistemasApiController(ISistemaServicio sistemaServicio, IUsuarioSistemaServicio usuarioSistemaServicio)
        {
            _sistemaServicio = sistemaServicio;
            _usuarioSistemaServicio = usuarioSistemaServicio;
        }

        [HttpGet("{idSistema}")]
        public async Task<IActionResult> ObtenerPorCodigo(int idSistema)
        {
            try
            {
                var sistema = await _sistemaServicio.ObtenerPorIdAsync(idSistema);

                if (sistema == null)
                {
                    return NotFound(new
                    {
                        exito = false,
                        mensaje = "Sistema no encontrado."
                    });
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Sistema encontrado.",
                    datos = new
                    {
                        sistema.IdSistema,
                        sistema.Codigo,
                        sistema.Nombre,
                        sistema.Activo
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al consultar el sistema."
                });
            }
        }

        [HttpGet("{idSistema}/usuarios")]
        public async Task<IActionResult> ObtenerUsuariosPorSistema(int idSistema)
        {
            try
            {
                var usuarios = await _usuarioSistemaServicio.ObtenerUsuariosPorSistemaAsync(idSistema);

                var lista = usuarios.ToList();

                if (!lista.Any())
                {
                    return Ok(new
                    {
                        exito = true,
                        mensaje = "No se encontraron usuarios para el sistema.",
                        datos = new List<object>()
                    });
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Usuarios obtenidos correctamente.",
                    datos = lista.Select(u => new
                    {
                        u.UsuarioId,
                        u.NombreCompleto,
                        u.Email
                    })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al obtener los usuarios del sistema."
                });
            }
        }

        [HttpGet("por-sistema-y-rol")]
        public async Task<IActionResult> ObtenerUsuariosPorSistemaYRol([FromQuery] int sistemaId, [FromQuery] string rolNombre)
        {
            try
            {
                var usuarios = await _usuarioSistemaServicio.ObtenerUsuariosPorSistemaYRolAsync(sistemaId, rolNombre);

                var lista = usuarios.ToList();

                if (!lista.Any())
                {
                    return Ok(new
                    {
                        exito = true,
                        mensaje = "No se encontraron usuarios.",
                        datos = new List<object>()
                    });
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Usuarios obtenidos correctamente.",
                    datos = usuarios
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al obtener los usuarios."
                });
            }
        }
    }
}
