using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Repositories;
using SASI.Models;
using SASI.Models.Requests;
using System.Data;
using X.PagedList.Extensions;

namespace SASI.Controllers
{
    [Authorize]
    public class ObjetoController : Controller
    {
        private readonly IObjetoRepository _objetoRepository;
        private readonly ISistemaRepository _sistemaRepository;

        public ObjetoController(IObjetoRepository objetoRepository, ISistemaRepository sistemaRepository)
        {
            _objetoRepository = objetoRepository;
            _sistemaRepository = sistemaRepository;
        }

        public async Task<IActionResult> Index(int idSistema, int? page)
        {
            int pageSize = 5;
            int pageNumber = page ?? 1;

            var sistema = await _sistemaRepository.ObtenerPorId(idSistema);
            if (sistema == null)
            {
                return NotFound(); // o redirigir a una vista de error
            }

            ViewBag.Sistema = sistema;

            var objetos = await _objetoRepository.ObtenerPorSistemaAsync(idSistema) ?? new List<Objeto>();

            var pagedObjetos = objetos.ToPagedList(pageNumber, pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(pagedObjetos);
        }

        [HttpGet]
        public async Task<IActionResult> Crear(int idSistema)
        {
            var objetosPadre = await _objetoRepository.ListarObjetosPadrePorSistemaAsync(idSistema);

            var viewModel = new ObjetoViewModel
            {
                IdSistema = idSistema,
                Activo = true,
                ObjetosPadre = objetosPadre.Select(o => new SelectListItem
                {
                    Value = o.IdObjeto.ToString(),
                    Text = o.Nombre
                })
            };

            return PartialView("_CrearObjetoPartial", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(ObjetoViewModel viewModel)
        {
            ModelState.Clear();

            if (!ModelState.IsValid)
            {
                // Si hay error, vuelve a cargar la lista de objetos padre
                var objetosPadre = await _objetoRepository.ListarObjetosPadrePorSistemaAsync(viewModel.IdSistema);
                viewModel.ObjetosPadre = objetosPadre.Select(o => new SelectListItem
                {
                    Value = o.IdObjeto.ToString(),
                    Text = o.Nombre
                });

                return PartialView("_CrearObjetoPartial", viewModel);
            }

            var objeto = new Objeto
            {
                IdSistema = viewModel.IdSistema,
                Nombre = viewModel.Nombre,
                Tipo = viewModel.Tipo,
                Url = viewModel.Url,
                Titulo = viewModel.Titulo,
                Icono = viewModel.Icono ?? string.Empty,
                Activo = viewModel.Activo,
                Orden = viewModel.Orden,
                IdPadre = viewModel.IdPadre
            };

            await _objetoRepository.CrearAsync(objeto);
            return Json(new { success = true });
        }

        public async Task<IActionResult> Editar(int id)
        {
            var objeto = await _objetoRepository.ObtenerPorIdAsync(id);
            if (objeto == null)
                return NotFound();

            var viewModel = new ObjetoViewModel
            {
                IdObjeto = objeto.IdObjeto,
                Nombre = objeto.Nombre,
                Tipo = objeto.Tipo,
                Url = objeto.Url,
                Titulo = objeto.Titulo,
                Icono = objeto.Icono,
                Orden = objeto.Orden,
                IdSistema = objeto.IdSistema,
                IdPadre = objeto.IdPadre,
                ObjetosPadre = await ObtenerObjetosPadreSelectListAsync(objeto.IdSistema, objeto.IdPadre)
            };

            return PartialView("_CrearObjetoPartial", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(ObjetoViewModel modelo)
        {
            ModelState.Clear();

            if (!ModelState.IsValid)
            {
                modelo.ObjetosPadre = await ObtenerObjetosPadreSelectListAsync(modelo.IdSistema);
                return PartialView("_CrearObjetoPartial", modelo);
            }
                
            var objeto = new Objeto
            {
                IdObjeto = modelo.IdObjeto,
                Nombre = modelo.Nombre,
                Tipo = modelo.Tipo,
                Url = modelo.Url,
                Titulo = modelo.Titulo,
                Icono = modelo.Icono ?? string.Empty,
                Orden = modelo.Orden,
                IdSistema = modelo.IdSistema,
                IdPadre = modelo.IdPadre,
                Activo = true
            };

            await _objetoRepository.ActualizarAsync(objeto);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado([FromBody] EliminarObjetoRequest request)
        {
            var objeto = await _objetoRepository.ObtenerPorIdAsync(request.Id);
            if (objeto == null)
                return Json(new { success = false });

            objeto.Activo = !objeto.Activo;
            await _objetoRepository.ActualizarAsync(objeto);

            return Json(new { success = true, estado = objeto.Activo });
        }

        public IActionResult ObjetosPorSistema(int idSistema)
        {
            var sistema = _sistemaRepository.ObtenerPorId(idSistema);
            var objetos = _objetoRepository.ObtenerPorSistemaAsync(idSistema);

            ViewBag.Sistema = sistema;
            return View("Objetos", objetos);
        }

        private async Task<List<SelectListItem>> ObtenerObjetosPadreSelectListAsync(int idSistema, int? idPadreSeleccionado = null)
        {
            var padres = await _objetoRepository.ListarObjetosPadrePorSistemaAsync(idSistema);
            return padres.Select(p => new SelectListItem
            {
                Value = p.IdObjeto.ToString(),
                Text = p.Nombre,
                Selected = idPadreSeleccionado.HasValue && p.IdObjeto == idPadreSeleccionado.Value
            }).ToList();
        }
    }
}
