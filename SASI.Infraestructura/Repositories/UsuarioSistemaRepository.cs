using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
//using SASI.DTO;
using SASI.Infraestructura.Identity;
using SistemaConvocatorias.Infraestructura.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class UsuarioSistemaRepository : IUsuarioSistemaRepository
    {
        private readonly SasiDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsuarioSistemaRepository(SasiDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ResultadoAsignacionUsuarioDto> AsignarUsuarioASistemaAsync(string usuarioId, int sistemaId, int rolId, bool esPrincipal)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);
            if (usuario == null)
                return new ResultadoAsignacionUsuarioDto { Exito = false, Mensaje = "Usuario no encontrado." };

            if (!Guid.TryParse(usuarioId, out Guid usuarioGuid))
                return new ResultadoAsignacionUsuarioDto { Exito = false, Mensaje = "Formato de ID de usuario inválido." };

            bool yaExiste = await _context.UsuarioSistemas.AnyAsync(us =>
                us.UsuarioId == usuarioGuid &&
                us.SistemaId == sistemaId &&
                us.RolId == rolId);

            if (yaExiste)
                return new ResultadoAsignacionUsuarioDto { Exito = false, Mensaje = "El usuario ya tiene asignado este rol en el sistema." };

            if (esPrincipal)
            {
                var asignaciones = await _context.UsuarioSistemas
                    .Where(us => us.UsuarioId == usuarioGuid && us.EsPrincipal)
                    .ToListAsync();

                foreach (var asignacion in asignaciones)
                {
                    asignacion.EsPrincipal = false;
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            var nuevaAsignacion = new UsuarioSistema
            {
                UsuarioId = usuarioGuid,
                SistemaId = sistemaId,
                RolId = rolId,
                FechaAsignacion = DateTime.Now,
                Activo = true,
                EsPrincipal = esPrincipal
            };

            _context.UsuarioSistemas.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ResultadoAsignacionUsuarioDto { Exito = true, Mensaje = "Asignación registrada correctamente." };
        }

        public async Task<List<SistemaAsignadoDto>> ObtenerSistemasPorUsuarioAsync(Guid usuarioId)
        {
            return await _context.UsuarioSistemas
                    .Include(us => us.Sistema)
                    .Include(us => us.Rol)
                    .Where(us => us.UsuarioId == usuarioId)
                    .OrderBy(us => us.Sistema.Nombre)
                    .ThenBy(us => us.Rol.Nombre)
                    .Select(us => new SistemaAsignadoDto
                    {
                        SistemaId = us.SistemaId,
                        NombreSistema = us.Sistema.Nombre,
                        RolId = us.RolId,
                        NombreRol = us.Rol.Nombre,
                        FechaAsignacion = us.FechaAsignacion,
                        Activo = us.Activo,
                        EsPrincipal = us.EsPrincipal
                    })
                    .ToListAsync();
        }

        public async Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaAsync(int sistemaId)
        {
            // 1. Obtener asignaciones activas
            var asignaciones = await _context.UsuarioSistemas
                .AsNoTracking()
                .Where(us => us.SistemaId == sistemaId && us.Activo)
                .ToListAsync();

            var rolIds = asignaciones.Select(a => a.RolId).Distinct().ToList();

            // 2. Obtener únicamente los usuarios asignados (consulta en BD con IN)
            var usuarioIds = asignaciones.Select(a => a.UsuarioId).Distinct().ToList();
            var usuarios = await _userManager.Users
                .AsNoTracking()
                .Where(u => usuarioIds.Contains(u.Id))
                .ToListAsync();

            // 3. Obtener roles
            var roles = await _context.Roles
                .Where(r => rolIds.Contains(r.IdRol))
                .ToListAsync();

            // 4. Mapear resultados
            var resultado = asignaciones.Select(asig =>
            {
                var user = usuarios.FirstOrDefault(u => u.Id == asig.UsuarioId);
                var rol = roles.FirstOrDefault(r => r.IdRol == asig.RolId);

                return new UsuarioAsignadoDto
                {
                    UsuarioId = asig.UsuarioId,
                    Email = user?.Email ?? "",
                    UserName = user?.UserName ?? "",
                    NombreCompleto = user?.NombreCompleto ?? "",
                    Rol = rol?.Nombre ?? "",
                    FechaAsignacion = asig.FechaAsignacion
                };
            }).ToList();

            return resultado;
        }

        public async Task<bool> QuitarUsuarioDeSistemaAsync(Guid usuarioId, int sistemaId)
        {
            var asignacion = await _context.UsuarioSistemas
                                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.SistemaId == sistemaId && x.Activo);

            if (asignacion == null)
                return false;

            asignacion.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ResultadoCambioEstadoDto> ActualizarEstadoSistemaAsync(Guid usuarioId, int sistemaId, int rolId, bool nuevoEstado)
        {
            var asignacion = await _context.UsuarioSistemas
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.SistemaId == sistemaId && x.RolId == rolId);

            if (asignacion == null)
                return ResultadoCambioEstadoDto.Error("No se encontró la asignación del sistema al usuario.");

            // Validar si se quiere desactivar y ese rol es el principal
            if (!nuevoEstado && asignacion.EsPrincipal)
                return ResultadoCambioEstadoDto.Error("No se puede desactivar un rol principal. Asigne otro rol principal antes de desactivarlo.");

            asignacion.Activo = nuevoEstado;
            await _context.SaveChangesAsync();
            return ResultadoCambioEstadoDto.Ok();
        }

        public async Task<List<UsuarioSistemaRolDto>> ObtenerSistemasYRolesDelUsuarioAsync(Guid userId)
        {
            // 1. Asignaciones del usuario (sistema + rol) en una sola consulta
            var asignaciones = await (
                    from us in _context.UsuarioSistemas
                    join sistema in _context.Sistemas on us.SistemaId equals sistema.IdSistema
                    join rol in _context.Roles on us.RolId equals rol.IdRol
                    where us.UsuarioId == userId
                    select new { us, sistema, rol })
                .AsNoTracking()
                .ToListAsync();

            if (!asignaciones.Any())
                return new List<UsuarioSistemaRolDto>();

            var rolIds = asignaciones.Select(a => a.rol.IdRol).Distinct().ToList();

            // 2. Objetos de todos los roles en una sola consulta (evita N+1 / subconsultas por rol)
            var objetosPorRol = await (
                    from ro in _context.RolObjetos
                    join obj in _context.Objetos on ro.IdObjeto equals obj.IdObjeto
                    where rolIds.Contains(ro.IdRol) && ro.Activo && obj.Activo
                    orderby obj.Orden
                    select new { ro.IdRol, Objeto = obj })
                .AsNoTracking()
                .ToListAsync();

            // 3. Componer el resultado en memoria
            return asignaciones.Select(a => new UsuarioSistemaRolDto
            {
                SistemaId = a.sistema.IdSistema,
                SistemaNombre = a.sistema.Nombre,
                SistemaActivo = a.sistema.Activo,
                RolId = a.rol.IdRol,
                RolNombre = a.rol.Nombre,
                RolActivo = a.rol.Activo,
                UsuarioSistemaRolActivo = a.us.Activo,
                EsPrincipal = a.us.EsPrincipal,
                Objetos = objetosPorRol
                    .Where(o => o.IdRol == a.rol.IdRol)
                    .Select(o => new ObjetoDto
                    {
                        IdObjeto = o.Objeto.IdObjeto,
                        Nombre = o.Objeto.Nombre,
                        Tipo = o.Objeto.Tipo,
                        Url = o.Objeto.Url ?? "",
                        Titulo = o.Objeto.Titulo ?? "",
                        Icono = o.Objeto.Icono ?? string.Empty,
                        Activo = o.Objeto.Activo,
                        Orden = o.Objeto.Orden,
                        IdPadre = o.Objeto.IdPadre
                    })
                    .ToList()
            }).ToList();
        }

        public async Task<bool> UsuarioTieneRolActivoEnSistemaAsync(Guid usuarioId, int sistemaId)
        {
            return await _context.UsuarioSistemas
                        .Include(us => us.Rol)
                        .AnyAsync(us =>
                            us.UsuarioId == usuarioId &&
                            us.SistemaId == sistemaId &&
                            us.Rol.Activo &&
                            us.Activo);
        }

        public async Task<List<Rol>> ObtenerRolesDelUsuarioEnSistema(Guid usuarioId, int sistemaId)
        {
            return await _context.UsuarioSistemas
                        .Include(us => us.Rol)
                        .Where(us => us.UsuarioId == usuarioId && us.SistemaId == sistemaId && us.Activo && us.Rol.Activo)
                        .Select(us => us.Rol)
                        .ToListAsync();
        }

        public async Task<int?> ObtenerRolPredeterminado(Guid idUsuario, int idSistema)
        {
            return await _context.UsuarioSistemas
                    .Where(us => us.UsuarioId == idUsuario && us.SistemaId == idSistema)
                    .OrderByDescending(us => us.EsPrincipal)
                    .Select(us => (int?)us.RolId)
                    .FirstOrDefaultAsync();
        }

        public async Task ActualizarRolPrincipalAsync(Guid usuarioId, int idSistema, int nuevoRolPrincipalId)
        {
            var asignaciones = await _context.UsuarioSistemas
                                .Where(us => us.UsuarioId == usuarioId && us.SistemaId == idSistema)
                                .ToListAsync();

            foreach (var a in asignaciones)
                a.EsPrincipal = (a.RolId == nuevoRolPrincipalId);

            await _context.SaveChangesAsync();
        }

        public async Task<List<UsuarioConRolesDto>> ObtenerUsuariosConRolesPorSistemaAsync(int sistemaId)
        {
            // 1. Obtener asignaciones activas
            var asignaciones = await _context.UsuarioSistemas
                .AsNoTracking()
                .Where(us => us.SistemaId == sistemaId && us.Activo)
                .ToListAsync();

            if (!asignaciones.Any())
                return new List<UsuarioConRolesDto>();

            var usuarioIds = asignaciones.Select(a => a.UsuarioId).Distinct().ToList();
            var rolIds = asignaciones.Select(a => a.RolId).Distinct().ToList();

            // 2. Obtener usuarios (consulta en BD con IN)
            var usuarios = await _userManager.Users
                .AsNoTracking()
                .Where(u => usuarioIds.Contains(u.Id))
                .ToListAsync();

            // 3. Obtener roles
            var roles = await _context.Roles
                .Where(r => rolIds.Contains(r.IdRol))
                .ToListAsync();

            // 4. Agrupar asignaciones por usuario
            var resultado = asignaciones
                .GroupBy(a => a.UsuarioId)
                .Select(g =>
                {
                    var user = usuarios.FirstOrDefault(u => u.Id == g.Key);
                    var nombresRoles = g.Select(a =>
                    {
                        var rol = roles.FirstOrDefault(r => r.IdRol == a.RolId);
                        return rol?.Nombre ?? "";
                    }).Distinct().ToList();

                    return new UsuarioConRolesDto
                    {
                        UsuarioId = g.Key,
                        Email = user?.Email ?? "",
                        UserName = user?.UserName ?? "",
                        NombreCompleto = user?.NombreCompleto ?? "",
                        Roles = nombresRoles,
                        FechaAsignacion = g.Min(a => a.FechaAsignacion) // o Max, depende de lo que necesites
                    };
                })
                .ToList();

            return resultado;
        }

        public async Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorSistemaYRolAsync(int sistemaId, string nombreRol)
        {
            // 1️⃣ Obtener asignaciones activas del sistema
            var asignaciones = await _context.UsuarioSistemas
                .AsNoTracking()
                .Where(us => us.SistemaId == sistemaId && us.Activo)
                .ToListAsync();

            if (!asignaciones.Any())
                return new List<UsuarioAsignadoDto>();

            // 2️⃣ Obtener IDs de roles asignados
            var rolIds = asignaciones
                .Select(a => a.RolId)
                .Distinct()
                .ToList();

            // 3️⃣ Obtener roles y filtrar por nombre
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => rolIds.Contains(r.IdRol) && r.Nombre == nombreRol)
                .ToListAsync();

            if (!roles.Any())
                return new List<UsuarioAsignadoDto>();

            var rolIdsFiltrados = roles.Select(r => r.IdRol).ToList();

            // 4️⃣ Filtrar asignaciones SOLO con el rol requerido
            var asignacionesFiltradas = asignaciones
                .Where(a => rolIdsFiltrados.Contains(a.RolId))
                .ToList();

            // 5️⃣ Obtener usuarios (Identity)
            var usuarioIds = asignacionesFiltradas
                .Select(a => a.UsuarioId)
                .Distinct()
                .ToList();

            var usuarios = await _userManager.Users
                .AsNoTracking()
                .Where(u => usuarioIds.Contains(u.Id))
                .ToListAsync();

            // 6️⃣ Mapear resultado
            var resultado = asignacionesFiltradas.Select(asig =>
            {
                var user = usuarios.FirstOrDefault(u => u.Id == asig.UsuarioId);
                var rol = roles.FirstOrDefault(r => r.IdRol == asig.RolId);

                return new UsuarioAsignadoDto
                {
                    UsuarioId = asig.UsuarioId,
                    Email = user?.Email ?? "",
                    UserName = user?.UserName ?? "",
                    NombreCompleto = user?.NombreCompleto ?? "",
                    Rol = rol?.Nombre ?? "",
                    FechaAsignacion = asig.FechaAsignacion
                };
            }).ToList();

            return resultado;
        }

    }
}
