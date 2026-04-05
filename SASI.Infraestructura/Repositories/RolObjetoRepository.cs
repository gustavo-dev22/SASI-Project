using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SistemaConvocatorias.Infraestructura.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class RolObjetoRepository : IRolObjetoRepository
    {
        private readonly SasiDbContext _context;

        public RolObjetoRepository(SasiDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarAsignacionesAsync(int idRol, List<int> idsAsignados)
        {
            var existentes = await _context.RolObjetos
                            .Where(x => x.IdRol == idRol)
                            .ToListAsync();

            // Desactivar todos los existentes
            foreach (var existente in existentes)
            {
                existente.Activo = false;
            }

            if (idsAsignados != null && idsAsignados.Any())
            {
                foreach (var idObjeto in idsAsignados)
                {
                    var existente = existentes.FirstOrDefault(x => x.IdObjeto == idObjeto);
                    if (existente != null)
                    {
                        existente.Activo = true;
                    }
                    else
                    {
                        _context.RolObjetos.Add(new RolObjeto
                        {
                            IdRol = idRol,
                            IdObjeto = idObjeto,
                            Activo = true
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task AsignarObjetoARolAsync(int idRol, int idObjeto)
        {
            var entity = new RolObjeto
            {
                IdRol = idRol,
                IdObjeto = idObjeto,
                Activo = true
            };
            await _context.RolObjetos.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsignacionesPorRolAsync(int idRol)
        {
            var existentes = await _context.RolObjetos
                            .Where(x => x.IdRol == idRol && x.Activo)
                            .ToListAsync();

            foreach (var asignacion in existentes)
            {
                asignacion.Activo = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> ObtenerIdsObjetosPorRolAsync(int idRol)
        {
            return await _context.RolObjetos
            .Where(ro => ro.IdRol == idRol && ro.Activo)
            .Select(ro => ro.IdObjeto)
            .ToListAsync();
        }
    }
}
