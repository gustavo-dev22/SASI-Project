using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Dominio.Repositories
{
    public interface IUsuarioSistemaRepository
    {
        Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaAsync(int sistemaId);
        Task<ResultadoAsignacionUsuarioDto> AsignarUsuarioASistemaAsync(string email, int sistemaId, int rolId, bool esPrincipal);
        Task<bool> QuitarUsuarioDeSistemaAsync(Guid usuarioId, int sistemaId);
        Task<List<SistemaAsignadoDto>> ObtenerSistemasPorUsuarioAsync(Guid usuarioId);
        Task<ResultadoCambioEstadoDto> ActualizarEstadoSistemaAsync(Guid usuarioId, int sistemaId, int rolId, bool nuevoEstado);
        Task<List<UsuarioSistemaRolDto>> ObtenerSistemasYRolesDelUsuarioAsync(Guid userId);
        Task<bool> UsuarioTieneRolActivoEnSistemaAsync(Guid usuarioId, int sistemaId);
        Task<List<Rol>> ObtenerRolesDelUsuarioEnSistema(Guid usuarioId, int sistemaId);
        Task<int?> ObtenerRolPredeterminado(Guid idUsuario, int idSistema);
        Task ActualizarRolPrincipalAsync(Guid usuarioId, int idSistema, int nuevoRolPrincipalId);
        Task<List<UsuarioConRolesDto>> ObtenerUsuariosConRolesPorSistemaAsync(int sistemaId);
        Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaYRolAsync(int sistemaId, string nombreRol);
    }
}
