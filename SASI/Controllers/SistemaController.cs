using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Helpers;
using SASI.Models;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize]
    public class SistemaController : Controller
    {
        private readonly ISistemaRepository _sistemaRepository;
        private readonly ICorrelativoRepository _correlativoRepository;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;

        public SistemaController(ISistemaRepository sistemaRepository, ICorrelativoRepository correlativoRepository, IUsuarioSistemaRepository usuarioSistemaRepository)
        {
            _sistemaRepository = sistemaRepository;
            _correlativoRepository = correlativoRepository;
            _usuarioSistemaRepository = usuarioSistemaRepository;
        }

        public async Task<IActionResult> Index(int? page)
        {
            var sistemas = await _sistemaRepository.ListarAsync(); // Esto debería incluir Estado y los roles

            // Proyección al ViewModel
            var sistemasViewModel = sistemas.Select(s => new SistemaViewModel
            {
                IdSistema = s.IdSistema,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                FechaRegistro = s.FechaRegistro,
                Estado = s.Activo,
                CantidadRoles = s.Roles?.Count() ?? 0 // Asegúrate de que Roles esté cargado si usas EF
            }).ToList();

            // Contar los activos
            ViewBag.Total = sistemasViewModel.Count();
            ViewBag.TotalActivos = sistemasViewModel.Count(s => s.Estado);
            ViewBag.TotalInactivos = sistemasViewModel.Count(s => !s.Estado);

            // Paginación
            int pageSize = 10;
            int pageNumber = page ?? 1;
            var pagedSistemas = sistemasViewModel
                .OrderByDescending(s => s.FechaRegistro)
                .ToPagedList(pageNumber, pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(pagedSistemas);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProximoCodigo()
        {
            var valorActual = await _correlativoRepository.ObtenerValorActualCorrelativo("Sistema");
            var siguiente = valorActual + 1;
            var codigo = $"SIS-{siguiente:D3}";
            return Ok(new { codigo });
        }

        public IActionResult Crear() => View();

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Sistema modelo)
        {
            ModelState.Clear();

            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos" });

            try
            {
                // Obtener siguiente número para la entidad "Sistema"
                int siguienteNumero = await _correlativoRepository.ObtenerSiguienteCorrelativoAsync("Sistema");
                string codigoGenerado = $"SIS-{siguienteNumero:D3}"; // SIS-0001, SIS-0002, etc.

                // Asignar código y fecha
                modelo.Codigo = codigoGenerado;
                modelo.FechaRegistro = DateTime.Now;

                await _sistemaRepository.CrearAsync(modelo);
                await _correlativoRepository.ActualizarCorrelativo("Sistema", siguienteNumero);

                return Ok(new { success = true, mensaje = "Sistema creado correctamente", codigo = modelo.Codigo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al crear el sistema." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _sistemaRepository.EliminarAsync(id);

            if (!resultado.Exito)
            {
                return Json(new { success = false, mensaje = resultado.Mensaje });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstado(int id)
        {
            var resultado = await _sistemaRepository.ActualizarEstadoAsync(id);

            return Json(new
            {
                success = resultado.Exito,
                message = resultado.Mensaje
            });
        }

        [HttpPost]
        public async Task<IActionResult> Editar([FromBody] Sistema sistema)
        {
            try
            {
                await _sistemaRepository.Actualizar(sistema);
                return Ok(new { success = true, mensaje = "Sistema editado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al editar el sistema." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var sistema = await _sistemaRepository.ObtenerPorId(id);
            if (sistema == null)
                return NotFound();

            return Json(sistema);
        }

        public async Task<IActionResult> UsuariosPorSistema(int sistemaId, int page = 1)
        {
            var pageSize = 5;

            var usuarios = await _usuarioSistemaRepository.ObtenerUsuariosConRolesPorSistemaAsync(sistemaId);

            var sistema = await _sistemaRepository.ObtenerPorId(sistemaId);

            ViewBag.SistemaId = sistemaId;
            ViewBag.NombreSistema = sistema.Nombre;

            var pagedUsuarios = usuarios.ToPagedList(page, pageSize);

            var html = await this.RenderViewAsync("_UsuariosPorSistemaPartial", pagedUsuarios, true);

            return Json(new
            {
                html,
                nombreSistema = sistema.Nombre
            });
        }
    }
}
