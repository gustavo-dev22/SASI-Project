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
    public class SistemaRepository : ISistemaRepository
    {
        private readonly SasiDbContext _context;

        public SistemaRepository(SasiDbContext context)
        {
            _context = context;
        }

        public async Task Actualizar(Sistema sistema)
        {
            var existente = await _context.Sistemas.FindAsync(sistema.IdSistema);
            if (existente != null)
            {
                existente.Codigo = sistema.Codigo;
                existente.Nombre = sistema.Nombre;
                existente.Descripcion = sistema.Descripcion;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CrearAsync(Sistema sistema)
        {
            sistema.FechaRegistro = DateTime.Now;
            _context.Sistemas.Add(sistema);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Exito, string Mensaje)> EliminarAsync(int id)
        {
            var tieneRoles = await _context.Roles.AnyAsync(r => r.IdSistema == id && r.Activo);
            if (tieneRoles)
            {
                return (false, "No se puede eliminar el sistema porque tiene roles registrados.");
            }

            var sistema = await _context.Sistemas.FindAsync(id);
            if (sistema == null)
            {
                return (false, "Sistema no encontrado.");
            }

            sistema.Activo = false;
            _context.Sistemas.Update(sistema);
            await _context.SaveChangesAsync();

            return (true, "");
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id)
        {
            var tieneRoles = await _context.Roles.AnyAsync(r => r.IdSistema == id && r.Activo);
            if (tieneRoles)
            {
                return (false, "No se puede desactivar el sistema porque tiene roles registrados.");
            }

            var sistema = await _context.Sistemas.FindAsync(id);
            if (sistema == null)
                return (false, "Sistema no encontrado.");

            sistema.Activo = !sistema.Activo;
            _context.Sistemas.Update(sistema);
            await _context.SaveChangesAsync();

            return (true, sistema.Activo ? "Sistema habilitado correctamente." : "Sistema deshabilitado correctamente.");
        }

        public async Task<List<Sistema>> ListarAsync()
        {
            return await _context.Sistemas.Include(s => s.Roles).OrderByDescending(s => s.FechaRegistro).ToListAsync();
        }

        public async Task<Sistema> ObtenerPorId(int id)
        {
            return await _context.Sistemas.FirstOrDefaultAsync(s => s.IdSistema == id);
        }
    }
}
