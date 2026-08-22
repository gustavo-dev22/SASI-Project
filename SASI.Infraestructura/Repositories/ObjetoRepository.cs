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
    public class ObjetoRepository : IObjetoRepository
    {
        private readonly SasiDbContext _context;

        public ObjetoRepository(SasiDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarAsync(Objeto objeto)
        {
            _context.Objetos.Update(objeto);
            await _context.SaveChangesAsync();
        }

        public async Task CrearAsync(Objeto objeto)
        {
            _context.Objetos.Add(objeto);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var objeto = await _context.Objetos.FindAsync(id);
            if (objeto != null)
            {
                objeto.Activo = false;
                _context.Objetos.Update(objeto);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistenObjetosParaSistema(int idSistema)
        {
            return await _context.Objetos.AnyAsync(o => o.IdSistema == idSistema && o.Activo);
        }

        public async Task<IEnumerable<Objeto>> ListarObjetosPadrePorSistemaAsync(int idSistema)
        {
            return await _context.Objetos
            .AsNoTracking()
            .Where(o => o.IdSistema == idSistema && o.IdPadre == null)
            .OrderBy(o => o.Orden)
            .ToListAsync();
        }

        public async Task<List<Objeto>> ObtenerPorIdsAsync(List<int> ids)
        {
            return await _context.Objetos
                .AsNoTracking()
                .Where(o => ids.Contains(o.IdObjeto))
                .ToListAsync();
        }

        public async Task<Objeto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Objetos.FindAsync(id);
        }

        public async Task<IEnumerable<Objeto>> ObtenerPorSistemaAsync(int idSistema)
        {
            return await _context.Objetos
            .AsNoTracking()
            .Where(o => o.IdSistema == idSistema)
            .OrderBy(o => o.Orden)
            .ToListAsync();
        }

        public async Task<IEnumerable<Objeto>> ObtenerPorSistemaYRolNombreAsync(int sistemaId, string rolNombre)
        {
            return await (from o in _context.Objetos
                          join ro in _context.RolObjetos on o.IdObjeto equals ro.IdObjeto
                          join r in _context.Roles on ro.IdRol equals r.IdRol
                          where r.IdSistema == sistemaId
                            && r.Nombre == rolNombre
                            && r.Activo == true
                            && o.Activo == true
                          orderby o.Orden ascending
                          select o)
                          .Distinct()
                          .ToListAsync();
        }
    }
}
