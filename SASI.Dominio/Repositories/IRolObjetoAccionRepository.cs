namespace SASI.Dominio.Repositories
{
    public interface IRolObjetoAccionRepository
    {
        Task<List<SASI.Dominio.Modelo.Accion>> ObtenerCatalogoAsync();
        Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolAsync(int idRol);
        Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolSistemaAsync(int idRol, int idSistema);
        Task<List<(int IdObjeto, int IdAccion)>> ObtenerAsignacionesPorRolAsync(int idRol);
        Task ActualizarAsignacionesAsync(int idRol, List<(int IdObjeto, int IdAccion)> asignaciones);
        Task<bool> ExistePermisoAsync(int idRol, int idObjeto, string codigoAccion);
        Task<bool> TieneAlgunaAccionConfiguradaAsync(int idRol, int idObjeto);
    }
}
