using Microsoft.AspNetCore.Mvc;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Repositories;

namespace SASI.Controllers.API
{
    [ApiController]
    [Route("api/sistemas")]
    public class SistemasApiController : Controller
    {
        private readonly ISistemaRepository _sistemaRepository;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;

        public SistemasApiController(ISistemaRepository sistemaRepository, IUsuarioSistemaRepository usuarioSistemaRepository)
        {
            _sistemaRepository = sistemaRepository;
            _usuarioSistemaRepository = usuarioSistemaRepository;
        }

        [HttpGet("{idSistema}")]
        public async Task<IActionResult> ObtenerPorCodigo(int idSistema)
        {
            try
            {
                var sistema = await _sistemaRepository.ObtenerPorId(idSistema);

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al consultar el sistema.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{idSistema}/usuarios")]
        public async Task<IActionResult> ObtenerUsuariosPorSistema(int idSistema)
        {
            try
            {
                var usuarios = await _usuarioSistemaRepository
                    .ObtenerUsuariosPorSistemaAsync(idSistema);

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al obtener los usuarios del sistema.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("por-sistema-y-rol")]
        public async Task<IActionResult> ObtenerUsuariosPorSistemaYRol([FromQuery] int sistemaId, [FromQuery] string rolNombre)
        {
            try
            {
                var usuarios = await _usuarioSistemaRepository.ObtenerUsuariosPorSistemaYRolAsync(sistemaId, rolNombre);

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al obtener los usuarios.",
                    error = ex.Message
                });
            }
        }
    }
}
