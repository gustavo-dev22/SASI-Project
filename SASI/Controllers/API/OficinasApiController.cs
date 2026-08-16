using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SASI.Aplicacion.Servicios;
using SASI.Infraestructura.Identity;
using SASI.Models.Requests;

namespace SASI.Controllers.API
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/oficinas")]
    public class OficinasApiController : Controller
    {
        private readonly IOficinaServicio _oficinaServicio;
        private readonly UserManager<ApplicationUser> _userManager;

        public OficinasApiController(IOficinaServicio oficinaServicio, UserManager<ApplicationUser> userManager)
        {
            _oficinaServicio = oficinaServicio;
            _userManager = userManager;
        }

        [HttpGet("activas")]
        public async Task<IActionResult> GetActivas()
        {
            try
            {
                var oficinas = await _oficinaServicio.ListarActivasAsync();

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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al consultar las oficinas."
                });
            }
        }

        [HttpGet("{id:int}/usuarios")]
        public async Task<IActionResult> ObtenerUsuariosPorOficina(int id)
        {
            try
            {
                var oficina = await _oficinaServicio.ObtenerPorIdAsync(id);
                if (oficina == null)
                {
                    return NotFound(new { exito = false, mensaje = "Oficina no encontrada." });
                }

                var usuarios = await _oficinaServicio.ObtenerUsuariosPorOficinaAsync(id);

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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al consultar usuarios de la oficina."
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

                // 🔹 Buscar oficina
                var oficina = await _oficinaServicio.ObtenerPorIdAsync(oficinaId);

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
                        NombreCompleto = usuario.NombreCompleto,
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Error al obtener remitente."
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
                var oficina = await _oficinaServicio.ObtenerPorIdAsync(r.OficinaId);

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

        [HttpPost("por-ids")]
        public async Task<IActionResult> GetOficinasPorIds([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return BadRequest(new { exito = false, mensaje = "Lista de IDs vacía." });

                var todas = await _oficinaServicio.ListarActivasAsync();
                var filtradas = todas.Where(o => ids.Contains(o.IdOficina)).Select(o => new {
                    o.IdOficina,
                    o.Nombre,
                    o.Sigla
                }).ToList();

                return Ok(new { exito = true, datos = filtradas });
            }
            catch (Exception)
            {
                return StatusCode(500, new { exito = false, mensaje = "Error al obtener las oficinas." });
            }
        }
    }
}
