using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;

namespace SASI.Aplicacion.Servicios
{
    public interface ISistemaServicio
    {
        Task<List<Sistema>> ListarAsync();
        Task<Sistema?> ObtenerPorIdAsync(int id);
        Task<string> ObtenerProximoCodigoAsync();
        Task<(bool Exito, string Mensaje, string? Codigo)> CrearAsync(Sistema modelo);
        Task<(bool Exito, string Mensaje)> ActualizarAsync(Sistema modelo);
        Task<(bool Exito, string Mensaje)> EliminarAsync(int id);
        Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id);
        Task<List<UsuarioConRolesDto>> ObtenerUsuariosConRolesPorSistemaAsync(int sistemaId);
    }

    public class SistemaServicio : ISistemaServicio
    {
        private readonly ISistemaRepository _sistemaRepository;
        private readonly ICorrelativoRepository _correlativoRepository;
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;

        public SistemaServicio(
            ISistemaRepository sistemaRepository,
            ICorrelativoRepository correlativoRepository,
            IUsuarioSistemaRepository usuarioSistemaRepository)
        {
            _sistemaRepository = sistemaRepository;
            _correlativoRepository = correlativoRepository;
            _usuarioSistemaRepository = usuarioSistemaRepository;
        }

        public async Task<List<Sistema>> ListarAsync()
            => await _sistemaRepository.ListarAsync();

        public async Task<Sistema?> ObtenerPorIdAsync(int id)
            => await _sistemaRepository.ObtenerPorId(id);

        public async Task<string> ObtenerProximoCodigoAsync()
        {
            var valorActual = await _correlativoRepository.ObtenerValorActualCorrelativo("Sistema");
            return $"SIS-{valorActual + 1:D3}";
        }

        public async Task<(bool Exito, string Mensaje, string? Codigo)> CrearAsync(Sistema modelo)
        {
            int siguienteNumero = await _correlativoRepository.ObtenerSiguienteCorrelativoAsync("Sistema");
            string codigoGenerado = $"SIS-{siguienteNumero:D3}";

            modelo.Codigo = codigoGenerado;
            modelo.FechaRegistro = DateTime.Now;

            await _sistemaRepository.CrearAsync(modelo);
            await _correlativoRepository.ActualizarCorrelativo("Sistema", siguienteNumero);

            return (true, "Sistema creado correctamente", codigoGenerado);
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarAsync(Sistema modelo)
        {
            await _sistemaRepository.Actualizar(modelo);
            return (true, "Sistema editado correctamente");
        }

        public async Task<(bool Exito, string Mensaje)> EliminarAsync(int id)
            => await _sistemaRepository.EliminarAsync(id);

        public async Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id)
            => await _sistemaRepository.ActualizarEstadoAsync(id);

        public async Task<List<UsuarioConRolesDto>> ObtenerUsuariosConRolesPorSistemaAsync(int sistemaId)
            => await _usuarioSistemaRepository.ObtenerUsuariosConRolesPorSistemaAsync(sistemaId);
    }
}
