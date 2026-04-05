using Microsoft.AspNetCore.Mvc;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Repositories;

namespace SASI.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObjetosApiController : Controller
    {
        private readonly IObjetoRepository _objetoRepository;
        private readonly IRolRepository _rolRepository;

        public ObjetosApiController(IObjetoRepository objetoRepository, IRolRepository rolRepository)
        {
            _objetoRepository = objetoRepository;
            _rolRepository = rolRepository;
        }

        [HttpGet("PorSistema/{idSistema}")]
        public async Task<IActionResult> ObtenerObjetosPorSistema(int idSistema)
        {
            try
            {
                var objetos = await _objetoRepository.ObtenerPorSistemaAsync(idSistema);

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
            catch (Exception ex)
            {
                // Aquí puedes loguear el error si tienes logger configurado

                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al consultar los objetos del sistema.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("RolesPorSistema/{idSistema}")]
        public async Task<IActionResult> ObtenerRolesPorSistema(int idSistema)
        {
            try
            {
                var roles = await _rolRepository.ObtenerPorSistemaId(idSistema);

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = "Ocurrió un error al consultar los roles del sistema.",
                    error = ex.Message
                });
            }
        }
    }
}
