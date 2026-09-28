using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;

namespace SASI.Aplicacion.Servicios
{
    public interface IPermisoServicio
    {
        Task<bool> TienePermisoAsync(int sistemaId, int rolId, int idObjeto, string codigoAccion);
        Task<bool> TienePermisoPorUrlAsync(int sistemaId, int rolId, string urlController, string codigoAccion);
        Task<bool> TieneAlgunaAccionConfiguradaAsync(int idRol, int idObjeto);
        Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolAsync(int idRol);
        Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolSistemaAsync(int idRol, int idSistema);
        Task<int?> ObtenerIdObjetoPorUrlAsync(int idSistema, string urlController);
        Task<List<Accion>> ObtenerCatalogoAccionesAsync();
    }

    public class PermisoServicio : IPermisoServicio
    {
        private readonly IRolObjetoAccionRepository _rolObjetoAccionRepository;
        private readonly IObjetoRepository _objetoRepository;

        public PermisoServicio(
            IRolObjetoAccionRepository rolObjetoAccionRepository,
            IObjetoRepository objetoRepository)
        {
            _rolObjetoAccionRepository = rolObjetoAccionRepository;
            _objetoRepository = objetoRepository;
        }

        public async Task<bool> TienePermisoAsync(int sistemaId, int rolId, int idObjeto, string codigoAccion)
        {
            var objeto = await _objetoRepository.ObtenerPorIdAsync(idObjeto);
            if (objeto == null || objeto.IdSistema != sistemaId)
                return false;

            return await _rolObjetoAccionRepository.ExistePermisoAsync(rolId, idObjeto, codigoAccion);
        }

        public async Task<bool> TienePermisoPorUrlAsync(int sistemaId, int rolId, string urlController, string codigoAccion)
        {
            if (string.IsNullOrWhiteSpace(urlController))
                return false;

            var idObjeto = await _objetoRepository.ObtenerIdObjetoPorUrlAsync(sistemaId, urlController);
            if (!idObjeto.HasValue)
                return false;

            return await _rolObjetoAccionRepository.ExistePermisoAsync(rolId, idObjeto.Value, codigoAccion);
        }

        public Task<bool> TieneAlgunaAccionConfiguradaAsync(int idRol, int idObjeto)
            => _rolObjetoAccionRepository.TieneAlgunaAccionConfiguradaAsync(idRol, idObjeto);

        public Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolAsync(int idRol)
            => _rolObjetoAccionRepository.ObtenerPermisosPorRolAsync(idRol);

        public Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolSistemaAsync(int idRol, int idSistema)
            => _rolObjetoAccionRepository.ObtenerPermisosPorRolSistemaAsync(idRol, idSistema);

        public Task<int?> ObtenerIdObjetoPorUrlAsync(int idSistema, string urlController)
            => _objetoRepository.ObtenerIdObjetoPorUrlAsync(idSistema, urlController);

        public Task<List<Accion>> ObtenerCatalogoAccionesAsync()
            => _rolObjetoAccionRepository.ObtenerCatalogoAsync();
    }
}
