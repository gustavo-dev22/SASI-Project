using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;

namespace SASI.Aplicacion.Servicios
{
    public interface IObjetoServicio
    {
        Task<List<Objeto>> ObtenerPorSistemaAsync(int idSistema);
        Task<Objeto?> ObtenerPorIdAsync(int id);
        Task CrearAsync(Objeto objeto);
        Task ActualizarAsync(Objeto objeto);
        Task<(bool Exito, bool Estado)> CambiarEstadoAsync(int id);
        Task<List<Objeto>> ListarObjetosPadrePorSistemaAsync(int idSistema);
        Task<List<Objeto>> ObtenerPorIdsAsync(List<int> ids);
        Task<bool> ExistenObjetosParaSistemaAsync(int idSistema);
        Task<IEnumerable<Objeto>> ObtenerPorSistemaYRolNombreAsync(int sistemaId, string rolNombre);
    }

    public class ObjetoServicio : IObjetoServicio
    {
        private readonly IObjetoRepository _objetoRepository;

        public ObjetoServicio(IObjetoRepository objetoRepository)
        {
            _objetoRepository = objetoRepository;
        }

        public async Task<List<Objeto>> ObtenerPorSistemaAsync(int idSistema)
            => (await _objetoRepository.ObtenerPorSistemaAsync(idSistema)).ToList();

        public async Task<Objeto?> ObtenerPorIdAsync(int id)
            => await _objetoRepository.ObtenerPorIdAsync(id);

        public Task CrearAsync(Objeto objeto) => _objetoRepository.CrearAsync(objeto);

        public Task ActualizarAsync(Objeto objeto) => _objetoRepository.ActualizarAsync(objeto);

        public async Task<(bool Exito, bool Estado)> CambiarEstadoAsync(int id)
        {
            var objeto = await _objetoRepository.ObtenerPorIdAsync(id);
            if (objeto == null) return (false, false);

            objeto.Activo = !objeto.Activo;
            await _objetoRepository.ActualizarAsync(objeto);
            return (true, objeto.Activo);
        }

        public async Task<List<Objeto>> ListarObjetosPadrePorSistemaAsync(int idSistema)
            => (await _objetoRepository.ListarObjetosPadrePorSistemaAsync(idSistema)).ToList();

        public async Task<List<Objeto>> ObtenerPorIdsAsync(List<int> ids)
            => await _objetoRepository.ObtenerPorIdsAsync(ids);

        public async Task<bool> ExistenObjetosParaSistemaAsync(int idSistema)
            => await _objetoRepository.ExistenObjetosParaSistema(idSistema);

        public async Task<IEnumerable<Objeto>> ObtenerPorSistemaYRolNombreAsync(int sistemaId, string rolNombre)
        {
            return await _objetoRepository.ObtenerPorSistemaYRolNombreAsync(sistemaId, rolNombre);
        }
    }
}
