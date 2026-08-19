using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.DTO;
using SASI.Infraestructura.Identity;
using SistemaConvocatorias.Infraestructura.Datos;

namespace SASI.Servicios
{
    public interface IReporteServicio
    {
        Task<List<ResumenSistemaReporteDto>> ResumenPorSistemaAsync();
        Task<List<SistemaSinRolesDto>> SistemasSinRolesAsync();
        Task<List<RolSinObjetosDto>> RolesSinObjetosAsync();
        Task<List<OficinaSinUsuariosDto>> OficinasSinUsuariosAsync();
    }

    public class ReporteServicio : IReporteServicio
    {
        private readonly SasiDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReporteServicio(SasiDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<ResumenSistemaReporteDto>> ResumenPorSistemaAsync()
        {
            var sistemas = await _context.Sistemas
                .OrderBy(s => s.Nombre)
                .ToListAsync();

            var rolesPorSistema = await _context.Roles
                .Where(r => r.Activo)
                .GroupBy(r => r.IdSistema)
                .Select(g => new { IdSistema = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.IdSistema, x => x.Cantidad);

            var usuariosPorSistema = await _context.UsuarioSistemas
                .Where(us => us.Activo)
                .GroupBy(us => us.SistemaId)
                .Select(g => new { IdSistema = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.IdSistema, x => x.Cantidad);

            var objetosPorSistema = await _context.Objetos
                .Where(o => o.Activo)
                .GroupBy(o => o.IdSistema)
                .Select(g => new { IdSistema = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.IdSistema, x => x.Cantidad);

            return sistemas.Select(s => new ResumenSistemaReporteDto
            {
                IdSistema = s.IdSistema,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Activo = s.Activo,
                CantidadRoles = rolesPorSistema.GetValueOrDefault(s.IdSistema),
                CantidadUsuarios = usuariosPorSistema.GetValueOrDefault(s.IdSistema),
                CantidadObjetos = objetosPorSistema.GetValueOrDefault(s.IdSistema)
            }).ToList();
        }

        public async Task<List<SistemaSinRolesDto>> SistemasSinRolesAsync()
        {
            return await _context.Sistemas
                .Where(s => !_context.Roles.Any(r => r.IdSistema == s.IdSistema && r.Activo))
                .OrderBy(s => s.Nombre)
                .Select(s => new SistemaSinRolesDto
                {
                    IdSistema = s.IdSistema,
                    Codigo = s.Codigo,
                    Nombre = s.Nombre,
                    Activo = s.Activo
                })
                .ToListAsync();
        }

        public async Task<List<RolSinObjetosDto>> RolesSinObjetosAsync()
        {
            return await (
                    from r in _context.Roles
                    join s in _context.Sistemas on r.IdSistema equals s.IdSistema
                    where !_context.RolObjetos.Any(ro => ro.IdRol == r.IdRol && ro.Activo)
                    orderby s.Nombre, r.Nombre
                    select new RolSinObjetosDto
                    {
                        IdRol = r.IdRol,
                        Nombre = r.Nombre,
                        IdSistema = r.IdSistema,
                        NombreSistema = s.Nombre,
                        Activo = r.Activo
                    })
                .ToListAsync();
        }

        public async Task<List<OficinaSinUsuariosDto>> OficinasSinUsuariosAsync()
        {
            var oficinas = await _context.Oficina.ToListAsync();

            var oficinasConUsuarios = await _userManager.Users
                .Where(u => u.IdOficina.HasValue)
                .Select(u => u.IdOficina!.Value)
                .Distinct()
                .ToListAsync();

            return oficinas
                .Where(o => !oficinasConUsuarios.Contains(o.IdOficina))
                .OrderBy(o => o.Nombre)
                .Select(o => new OficinaSinUsuariosDto
                {
                    IdOficina = o.IdOficina,
                    Nombre = o.Nombre,
                    Sigla = o.Sigla,
                    Activo = o.Activo
                })
                .ToList();
        }
    }
}
