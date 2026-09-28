using Microsoft.AspNetCore.Mvc.Rendering;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;

namespace SASI.Aplicacion.Servicios
{
    public interface IRolServicio
    {
        Task<IEnumerable<Rol>> ObtenerPorSistemaIdAsync(int sistemaId);
        Task<Rol?> ObtenerPorIdAsync(int id);
        Task CrearAsync(Rol rol);
        Task EditarAsync(Rol rol);
        Task<(bool Exito, bool Estado)> CambiarEstadoAsync(int id);
        Task<List<SelectListItem>> ObtenerRolesComoSelectListAsync(int sistemaId);
        int ObtenerIdSistemaPorRol(int idRol);
        Task<List<Objeto>> ObtenerObjetosPorSistemaAsync(int sistemaId);
        Task<List<int>> ObtenerIdsObjetosPorRolAsync(int idRol);
        Task GuardarAsignacionObjetosAsync(int idRol, List<int> idsAsignados);
        Task<bool> ExistenObjetosParaSistemaAsync(int idSistema);
        Task<Dictionary<int, List<string>>> ObtenerAccionesPorRolAsync(int idRol);
        Task<List<(int IdObjeto, int IdAccion)>> ObtenerAsignacionesAccionesPorRolAsync(int idRol);
        Task GuardarAsignacionAccionesAsync(int idRol, List<(int IdObjeto, int IdAccion)> asignaciones);
    }

    public class RolServicio : IRolServicio
    {
        private readonly IRolRepository _rolRepository;
        private readonly IObjetoRepository _objetoRepository;
        private readonly IRolObjetoRepository _rolObjetoRepository;
        private readonly IRolObjetoAccionRepository _rolObjetoAccionRepository;

        public RolServicio(
            IRolRepository rolRepository,
            IObjetoRepository objetoRepository,
            IRolObjetoRepository rolObjetoRepository,
            IRolObjetoAccionRepository rolObjetoAccionRepository)
        {
            _rolRepository = rolRepository;
            _objetoRepository = objetoRepository;
            _rolObjetoRepository = rolObjetoRepository;
            _rolObjetoAccionRepository = rolObjetoAccionRepository;
        }

        public async Task<IEnumerable<Rol>> ObtenerPorSistemaIdAsync(int sistemaId)
            => await _rolRepository.ObtenerPorSistemaId(sistemaId);

        public async Task<Rol?> ObtenerPorIdAsync(int id)
            => await _rolRepository.ObtenerPorId(id);

        public Task CrearAsync(Rol rol) => _rolRepository.Crear(rol);

        public Task EditarAsync(Rol rol) => _rolRepository.Editar(rol);

        public async Task<(bool Exito, bool Estado)> CambiarEstadoAsync(int id)
        {
            var rol = await _rolRepository.ObtenerPorId(id);
            if (rol == null) return (false, false);

            rol.Activo = !rol.Activo;
            await _rolRepository.Editar(rol);
            return (true, rol.Activo);
        }

        public async Task<List<SelectListItem>> ObtenerRolesComoSelectListAsync(int sistemaId)
            => await _rolRepository.ObtenerRolesComoSelectListAsync(sistemaId);

        public int ObtenerIdSistemaPorRol(int idRol)
            => _rolRepository.ObtenerIdSistemaPorRol(idRol);

        public async Task<List<Objeto>> ObtenerObjetosPorSistemaAsync(int sistemaId)
            => (await _objetoRepository.ObtenerPorSistemaAsync(sistemaId)).ToList();

        public async Task<List<int>> ObtenerIdsObjetosPorRolAsync(int idRol)
            => await _rolObjetoRepository.ObtenerIdsObjetosPorRolAsync(idRol);

        public Task GuardarAsignacionObjetosAsync(int idRol, List<int> idsAsignados)
            => _rolObjetoRepository.ActualizarAsignacionesAsync(idRol, idsAsignados);

        public async Task<bool> ExistenObjetosParaSistemaAsync(int idSistema)
            => await _objetoRepository.ExistenObjetosParaSistema(idSistema);

        public Task<Dictionary<int, List<string>>> ObtenerAccionesPorRolAsync(int idRol)
            => _rolObjetoAccionRepository.ObtenerPermisosPorRolAsync(idRol);

        public Task<List<(int IdObjeto, int IdAccion)>> ObtenerAsignacionesAccionesPorRolAsync(int idRol)
            => _rolObjetoAccionRepository.ObtenerAsignacionesPorRolAsync(idRol);

        public Task GuardarAsignacionAccionesAsync(int idRol, List<(int IdObjeto, int IdAccion)> asignaciones)
            => _rolObjetoAccionRepository.ActualizarAsignacionesAsync(idRol, asignaciones);
    }
}
