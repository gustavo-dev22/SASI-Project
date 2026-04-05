using Microsoft.EntityFrameworkCore;
using SASI.Dominio.DTO;
using SASI.Dominio.Modelo;
using SASI.Dominio.Repositories;
using SASI.Infraestructura.Identity;
using SistemaConvocatorias.Infraestructura.Datos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASI.Infraestructura.Repositories
{
    public class OficinaRepository : IOficinaRepository
    {
        private readonly SasiDbContext _context;
        private readonly IdentityDbContext _identityContext;

        public OficinaRepository(SasiDbContext context, IdentityDbContext identityDbContext)
        {
            _context = context;
            _identityContext = identityDbContext;
        }

        public async Task Actualizar(Oficina oficina)
        {
            var existente = await _context.Oficina.FindAsync(oficina.IdOficina);
            if (existente != null)
            {
                existente.IdOficina = oficina.IdOficina;
                existente.Nombre = oficina.Nombre;
                existente.Sigla = oficina.Sigla;
                existente.IdOficinaPadre = oficina.IdOficinaPadre;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarEstadoAsync(int id)
        {
            var oficina = await _context.Oficina.FindAsync(id);
            if (oficina == null)
                return (false, "Oficina no encontrada.");

            oficina.Activo = !oficina.Activo;
            _context.Oficina.Update(oficina);
            await _context.SaveChangesAsync();

            return (true, oficina.Activo ? "Oficina habilitada correctamente." : "Oficina deshabilitada correctamente.");
        }

        public async Task CrearAsync(Oficina oficina)
        {
            _context.Oficina.Add(oficina);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Oficina>> ListarActivasAsync()
        {
            return await _context.Oficina
                    .Where(o => o.Activo)
                    .ToListAsync();
        }

        public async Task<List<Oficina>> ListarAsync()
        {
            return await _context.Oficina.ToListAsync();
        }

        public async Task<Oficina> ObtenerPorId(int id)
        {
            return await _context.Oficina.FirstOrDefaultAsync(s => s.IdOficina == id);
        }

        public async Task<Oficina> ObtenerPorNombre(string nombre)
        {
            var nombreNormalizado = nombre.ToLower().Trim();

            return await _context.Oficina
                    .FirstOrDefaultAsync(s =>
                    EF.Functions.Collate(s.Nombre.ToLower(), "SQL_Latin1_General_CP1_CI_AI") == nombreNormalizado);
        }

        public async Task<List<UsuarioAsignadoDto>> ObtenerUsuariosPorOficinaAsync(int idOficina)
        {
            // Traer usuarios filtrados por IdOficina
            var usuarios = await _identityContext.Users
                .AsNoTracking()
                .Where(u => u.IdOficina == idOficina) // tu columna agregada en AspNetUsers
                .Select(u => new UsuarioAsignadoDto
                {
                    UsuarioId = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    UserName = u.UserName
                })
                .ToListAsync();

            return usuarios;
        }
    }
}