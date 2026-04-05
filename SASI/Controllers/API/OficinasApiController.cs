using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using SASI.Infraestructura.Repositories;
using SASI.Models.Requests;

namespace SASI.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class OficinasApiController : Controller
    {
        private readonly IOficinaRepository _oficinaRepository;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public OficinasApiController(IOficinaRepository oficinaRepository, IUsuarioSistemaRepository usuarioSistemaRepository, UserManager<ApplicationUser> userManager)
        {
            _oficinaRepository = oficinaRepository;
            _usuarioSistemaRepository = usuarioSistemaRepository;
            _userManager = userManager;
        }

        [HttpGet("activas")]
        public async Task<IActionResult> GetActivas()
        {
            try
            {
                var oficinas = await _oficinaRepository.ListarActivasAsync();

                return Ok(new
                {
                    exito = true,
                    mensaje = "Listado de oficinas obtenido.",
                    datos = oficinas.Select(o => new
                    {
                        o.IdOficina,
                        o.Nombre,
                        o.Sigla,
                        o.IdOficinaPadre,
                        o.Activo
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al consultar las oficinas.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id:int}/usuarios")]
        public async Task<IActionResult> ObtenerUsuariosPorOficina(int id)
        {
            try
            {
                var oficina = await _oficinaRepository.ObtenerPorId(id);
                if (oficina == null)
                {
                    return NotFound(new { exito = false, mensaje = "Oficina no encontrada." });
                }

                var usuarios = await _oficinaRepository.ObtenerUsuariosPorOficinaAsync(id);

                if (usuarios == null || !usuarios.Any())
                {
                    return Ok(new
                    {
                        exito = true,
                        mensaje = "No se encontraron usuarios para esta oficina.",
                        datos = new List<object>()
                    });
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Usuarios de la oficina obtenidos.",
                    datos = usuarios.Select(u => new
                    {
                        u.UsuarioId,
                        u.NombreCompleto,
                        u.UserName
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al consultar usuarios de la oficina.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{usuarioId}/{oficinaId}")]
        public async Task<IActionResult> ObtenerRemitente(string usuarioId, int oficinaId)
        {
            try
            {
                // 🔹 Buscar usuario con Identity
                var usuario = await _userManager.FindByIdAsync(usuarioId);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        exito = false,
                        mensaje = "Usuario no encontrado."
                    });
                }

                // 🔹 Buscar oficina con tu repositorio de oficinas
                var oficina = await _oficinaRepository.ObtenerPorId(oficinaId);

                if (oficina == null)
                {
                    return NotFound(new
                    {
                        exito = false,
                        mensaje = "Oficina no encontrada."
                    });
                }

                // 🔹 Construcción del remitente
                var remitente = new
                {
                    Usuario = new
                    {
                        UsuarioId = usuario.Id,
                        NombreCompleto = usuario.NombreCompleto, // 👈 asegúrate que tu modelo ApplicationUser tenga esta propiedad
                        UserName = usuario.UserName
                    },
                    Oficina = new
                    {
                        oficina.IdOficina,
                        oficina.Nombre
                    }
                };

                return Ok(new
                {
                    exito = true,
                    mensaje = "Remitente obtenido correctamente.",
                    datos = remitente
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al obtener remitente.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("remitentes")]
        public async Task<IActionResult> GetRemitentes([FromBody] List<RemitenteRequest> remitentes)
        {
            if (remitentes == null || !remitentes.Any())
                return BadRequest(new { exito = false, mensaje = "Lista vacía." });

            var resultado = new List<object>();

            foreach (var r in remitentes)
            {
                var usuario = await _userManager.FindByIdAsync(r.UsuarioId.ToString());
                var oficina = await _oficinaRepository.ObtenerPorId(r.OficinaId);

                if (usuario != null && oficina != null)
                {
                    resultado.Add(new
                    {
                        usuario = new
                        {
                            usuarioId = usuario.Id,
                            nombreCompleto = usuario.NombreCompleto,
                            userName = usuario.UserName
                        },
                        oficina = new
                        {
                            idOficina = oficina.IdOficina,
                            nombre = oficina.Nombre
                        }
                    });
                }
            }

            return Ok(new
            {
                exito = true,
                mensaje = "Remitentes obtenidos correctamente.",
                datos = resultado
            });
        }
    }
}
