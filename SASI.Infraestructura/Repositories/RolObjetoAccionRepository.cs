using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SistemaConvocatorias.Infraestructura.Datos;

namespace SASI.Infraestructura.Repositories
{
    public class RolObjetoAccionRepository : IRolObjetoAccionRepository
    {
        private readonly SasiDbContext _context;

        public RolObjetoAccionRepository(SasiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Accion>> ObtenerCatalogoAsync()
        {
            return await _context.Acciones
                .AsNoTracking()
                .Where(a => a.Activo)
                .OrderBy(a => a.Orden)
                .ToListAsync();
        }

        public async Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolAsync(int idRol)
        {
            var query = await (from roa in _context.RolObjetoAcciones
                               join acc in _context.Acciones on roa.IdAccion equals acc.IdAccion
                               where roa.IdRol == idRol && roa.Activo && acc.Activo
                               select new { roa.IdObjeto, acc.Codigo })
                               .AsNoTracking()
                               .ToListAsync();

            return query
                .GroupBy(x => x.IdObjeto)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Codigo).Distinct().ToList());
        }

        public async Task<Dictionary<int, List<string>>> ObtenerPermisosPorRolSistemaAsync(int idRol, int idSistema)
        {
            var query = await (from roa in _context.RolObjetoAcciones
                               join acc in _context.Acciones on roa.IdAccion equals acc.IdAccion
                               join obj in _context.Objetos on roa.IdObjeto equals obj.IdObjeto
                               where roa.IdRol == idRol && roa.Activo && acc.Activo
                                     && obj.IdSistema == idSistema
                               select new { roa.IdObjeto, acc.Codigo })
                               .AsNoTracking()
                               .ToListAsync();

            return query
                .GroupBy(x => x.IdObjeto)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Codigo).Distinct().ToList());
        }

        public async Task<List<(int IdObjeto, int IdAccion)>> ObtenerAsignacionesPorRolAsync(int idRol)
        {
            var asignaciones = await _context.RolObjetoAcciones
                .AsNoTracking()
                .Where(x => x.IdRol == idRol && x.Activo)
                .Select(x => new { x.IdObjeto, x.IdAccion })
                .ToListAsync();

            return asignaciones.Select(x => (x.IdObjeto, x.IdAccion)).ToList();
        }

        public async Task ActualizarAsignacionesAsync(int idRol, List<(int IdObjeto, int IdAccion)> asignaciones)
        {
            var existentes = await _context.RolObjetoAcciones
                .Where(x => x.IdRol == idRol)
                .ToListAsync();

            foreach (var existente in existentes)
            {
                existente.Activo = false;
            }

            if (asignaciones != null && asignaciones.Any())
            {
                foreach (var (idObjeto, idAccion) in asignaciones)
                {
                    var existente = existentes.FirstOrDefault(x => x.IdObjeto == idObjeto && x.IdAccion == idAccion);
                    if (existente != null)
                    {
                        existente.Activo = true;
                    }
                    else
                    {
                        _context.RolObjetoAcciones.Add(new RolObjetoAccion
                        {
                            IdRol = idRol,
                            IdObjeto = idObjeto,
                            IdAccion = idAccion,
                            Activo = true
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistePermisoAsync(int idRol, int idObjeto, string codigoAccion)
        {
            return await (from roa in _context.RolObjetoAcciones
                          join acc in _context.Acciones on roa.IdAccion equals acc.IdAccion
                          where roa.IdRol == idRol && roa.IdObjeto == idObjeto
                                && roa.Activo && acc.Activo && acc.Codigo == codigoAccion
                          select 1).AnyAsync();
        }

        public async Task<bool> TieneAlgunaAccionConfiguradaAsync(int idRol, int idObjeto)
        {
            return await _context.RolObjetoAcciones
                .AnyAsync(x => x.IdRol == idRol && x.IdObjeto == idObjeto && x.Activo);
        }
    }
}
