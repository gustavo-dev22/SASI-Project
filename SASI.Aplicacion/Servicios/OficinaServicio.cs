using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;

namespace SASI.Aplicacion.Servicios
{
    public interface IOficinaServicio
    {
        Task<List<Oficina>> ListarAsync();
        Task<IEnumerable<Oficina>> ListarActivasAsync();
        Task<Oficina?> ObtenerPorIdAsync(int id);
        Task<Oficina?> ObtenerPorNombreAsync(string nombre);
        Task CrearAsync(Oficina oficina);
        Task ActualizarAsync(Oficina oficina);
        Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id);
        Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorOficinaAsync(int idOficina);
    }

    public class OficinaServicio : IOficinaServicio
    {
        private readonly IOficinaRepository _oficinaRepository;

        public OficinaServicio(IOficinaRepository oficinaRepository)
        {
            _oficinaRepository = oficinaRepository;
        }

        public async Task<List<Oficina>> ListarAsync()
            => await _oficinaRepository.ListarAsync();

        public async Task<IEnumerable<Oficina>> ListarActivasAsync()
            => await _oficinaRepository.ListarActivasAsync();

        public async Task<Oficina?> ObtenerPorIdAsync(int id)
            => await _oficinaRepository.ObtenerPorId(id);

        public async Task<Oficina?> ObtenerPorNombreAsync(string nombre)
            => await _oficinaRepository.ObtenerPorNombre(nombre);

        public Task CrearAsync(Oficina oficina) => _oficinaRepository.CrearAsync(oficina);

        public Task ActualizarAsync(Oficina oficina) => _oficinaRepository.Actualizar(oficina);

        public async Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id)
            => await _oficinaRepository.ActualizarEstadoAsync(id);

        public async Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorOficinaAsync(int idOficina)
            => await _oficinaRepository.ObtenerUsuariosPorOficinaAsync(idOficina);
    }
}
