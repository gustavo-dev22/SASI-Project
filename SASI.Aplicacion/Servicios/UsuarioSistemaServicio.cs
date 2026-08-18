using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;

namespace SASI.Aplicacion.Servicios
{
    public interface IUsuarioSistemaServicio
    {
        Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaAsync(int sistemaId);
        Task<List<UsuarioConRolesDto>> ObtenerUsuariosConRolesPorSistemaAsync(int sistemaId);
        Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaYRolAsync(int sistemaId, string nombreRol);
        Task<ResultadoAsignacionUsuarioDto> AsignarUsuarioASistemaAsync(string usuarioId, int sistemaId, int rolId, bool esPrincipal);
        Task<bool> QuitarUsuarioDeSistemaAsync(Guid usuarioId, int sistemaId);
        Task<ResultadoCambioEstadoDto> QuitarRolUsuarioDeSistemaAsync(Guid usuarioId, int sistemaId, int rolId);
        Task<ResultadoCambioEstadoDto> ActualizarEstadoSistemaAsync(Guid usuarioId, int sistemaId, int rolId, bool nuevoEstado);
        Task<List<SistemaAsignadoDto>> ObtenerSistemasPorUsuarioAsync(Guid usuarioId);
        Task ActualizarRolPrincipalAsync(Guid usuarioId, int idSistema, int nuevoRolPrincipalId);
        Task<bool> UsuarioTieneRolActivoEnSistemaAsync(Guid usuarioId, int sistemaId);
        Task<int?> ObtenerRolPredeterminadoAsync(Guid idUsuario, int idSistema);
        Task<List<UsuarioSistemaRolDto>> ObtenerSistemasYRolesDelUsuarioAsync(Guid userId);
        Task<List<Rol>> ObtenerRolesDelUsuarioEnSistemaAsync(Guid usuarioId, int sistemaId);
    }

    public class UsuarioSistemaServicio : IUsuarioSistemaServicio
    {
        private readonly IUsuarioSistemaRepository _usuarioSistemaRepository;

        public UsuarioSistemaServicio(IUsuarioSistemaRepository usuarioSistemaRepository)
        {
            _usuarioSistemaRepository = usuarioSistemaRepository;
        }

        public Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaAsync(int sistemaId)
            => _usuarioSistemaRepository.ObtenerUsuariosPorSistemaAsync(sistemaId);

        public Task<List<UsuarioConRolesDto>> ObtenerUsuariosConRolesPorSistemaAsync(int sistemaId)
            => _usuarioSistemaRepository.ObtenerUsuariosConRolesPorSistemaAsync(sistemaId);

        public Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaYRolAsync(int sistemaId, string nombreRol)
            => _usuarioSistemaRepository.ObtenerUsuariosPorSistemaYRolAsync(sistemaId, nombreRol);

        public Task<ResultadoAsignacionUsuarioDto> AsignarUsuarioASistemaAsync(string usuarioId, int sistemaId, int rolId, bool esPrincipal)
            => _usuarioSistemaRepository.AsignarUsuarioASistemaAsync(usuarioId, sistemaId, rolId, esPrincipal);

        public Task<bool> QuitarUsuarioDeSistemaAsync(Guid usuarioId, int sistemaId)
            => _usuarioSistemaRepository.QuitarUsuarioDeSistemaAsync(usuarioId, sistemaId);

        public Task<ResultadoCambioEstadoDto> QuitarRolUsuarioDeSistemaAsync(Guid usuarioId, int sistemaId, int rolId)
            => _usuarioSistemaRepository.QuitarRolUsuarioDeSistemaAsync(usuarioId, sistemaId, rolId);

        public Task<ResultadoCambioEstadoDto> ActualizarEstadoSistemaAsync(Guid usuarioId, int sistemaId, int rolId, bool nuevoEstado)
            => _usuarioSistemaRepository.ActualizarEstadoSistemaAsync(usuarioId, sistemaId, rolId, nuevoEstado);

        public Task<List<SistemaAsignadoDto>> ObtenerSistemasPorUsuarioAsync(Guid usuarioId)
            => _usuarioSistemaRepository.ObtenerSistemasPorUsuarioAsync(usuarioId);

        public Task ActualizarRolPrincipalAsync(Guid usuarioId, int idSistema, int nuevoRolPrincipalId)
            => _usuarioSistemaRepository.ActualizarRolPrincipalAsync(usuarioId, idSistema, nuevoRolPrincipalId);

        public Task<bool> UsuarioTieneRolActivoEnSistemaAsync(Guid usuarioId, int sistemaId)
            => _usuarioSistemaRepository.UsuarioTieneRolActivoEnSistemaAsync(usuarioId, sistemaId);

        public Task<int?> ObtenerRolPredeterminadoAsync(Guid idUsuario, int idSistema)
            => _usuarioSistemaRepository.ObtenerRolPredeterminado(idUsuario, idSistema);

        public Task<List<UsuarioSistemaRolDto>> ObtenerSistemasYRolesDelUsuarioAsync(Guid userId)
            => _usuarioSistemaRepository.ObtenerSistemasYRolesDelUsuarioAsync(userId);

        public Task<List<Rol>> ObtenerRolesDelUsuarioEnSistemaAsync(Guid usuarioId, int sistemaId)
            => _usuarioSistemaRepository.ObtenerRolesDelUsuarioEnSistema(usuarioId, sistemaId);
    }
}
