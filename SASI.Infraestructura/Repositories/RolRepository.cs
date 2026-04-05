using Microsoft.EntityFrameworkCore;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SistemaConvocatorias.Infraestructura.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SASI.Infraestructura.Repositories
{
    public class RolRepository : IRolRepository
    {
        private readonly SasiDbContext _context;

        public RolRepository(SasiDbContext context)
        {
            _context = context;
        }

        public async Task Crear(Rol rol)
        {
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();
        }

        public async Task Editar(Rol rol)
        {
            _context.Roles.Update(rol);
            await _context.SaveChangesAsync();
        }

        public async Task Eliminar(int id)
        {
            var rol = await _context.Roles.FindAsync(id);
            if (rol != null)
            {
                rol.Activo = false;
                _context.Roles.Update(rol);
                await _context.SaveChangesAsync();
            }
        }

        public int ObtenerIdSistemaPorRol(int idRol)
        {
            return _context.Roles
                    .Where(r => r.IdRol == idRol)
                    .Select(r => r.IdSistema)
                    .FirstOrDefault();
        }

        public async Task<Rol> ObtenerPorId(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<IEnumerable<Rol>> ObtenerPorSistemaId(int sistemaId)
        {
            return await _context.Roles
                    .Where(r => r.IdSistema == sistemaId)
                    .ToListAsync();
        }

        public async Task<List<SelectListItem>> ObtenerRolesComoSelectListAsync(int sistemaId)
        {
            return await _context.Roles
                        .Where(r => r.IdSistema == sistemaId)
                        .Select(r => new SelectListItem
                        {
                            Value = r.IdRol.ToString(),
                            Text = r.Nombre
                        }).ToListAsync();
        }
    }
}
